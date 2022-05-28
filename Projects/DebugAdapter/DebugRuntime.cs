using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using Runtime.IR;
using Runtime.IR.RuntimeTypes;

namespace DebugAdapter
{
    using RTE = Runtime.RTE;
    using StackFrame = Runtime.StackFrame;
    public sealed class DebugRuntime
    {
        private abstract record State
        {
            public sealed record Uninitialized : State { }
            public sealed record Running(bool IgnoreBreak, bool SingleStep) : State { }
            public sealed record Stopped : State{ }
            public sealed record Terminated : State{ }
        }

        public readonly CompiledModule Module;

        private readonly DebugAdapter _adapter;
        private readonly RTE _runtime;

        private readonly BlockingCollection<Action<DebugRuntime>> _runtimeRequests = new();

        private readonly PouId _entryPoint;

        private ImmutableArray<StackFrame>? _cachedStacktrace;

        private State _state = new State.Uninitialized();

        public DebugRuntime(
            DebugAdapter adapter,
            RTE runtime,
            CompiledModule module,
            PouId entryPoint)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            Module = module;
            _entryPoint = entryPoint;
        }

        private void Launch()
        {
            if (_state is State.Uninitialized)
            {
                _state = new State.Running(false, false);
                _runtime.Call(_entryPoint);
                foreach(var globalVarList in Module.GlobalVariableLists.OrderByDescending(x => x.Name))
                {
                    if (globalVarList.Initializer is CompiledPou initializer)
                        _runtime.Call(initializer);
                }
                _adapter.Protocol.SendEvent(new InitializedEvent());
            }
        }
        private void Pause()
        {
            Stop(new StoppedEvent(StoppedEvent.ReasonValue.Pause)
            {
                AllThreadsStopped = true,
                ThreadId = 0,
            });
        }
        private void Stop(StoppedEvent reason)
        {
            if (_state is State.Running)
            {
                _state = new State.Stopped();
                _adapter.Protocol.SendEvent(reason);
            }
        }
        private void Terminate()
        {
            _state = new State.Uninitialized();
            _adapter.Protocol.SendEvent(new ExitedEvent());
        }
        private void Continue(ImmutableArray<(PouId, int)> nextLocations)
        {
            _runtime.SetTemporaryBreakpoints(nextLocations);
            _state = new State.Running(true, false);
        }
        private void StepIn(ImmutableArray<(PouId, int)> nextLocations, int frameId, PouId? callee)
        {
            _runtime.SetTemporaryStepInBreakpoint(nextLocations, frameId, callee);
            _state = new State.Running(true, false);
        }
        private void StepSingle()
        {
            _state = new State.Running(true, true);
        }

        public void Run()
        {
            while (SingleCycle())
            {
                if(_runtimeRequests.TryTake(out var action))
                    action(this);
            }
        
            bool SingleCycle()
            {
                switch (_state)
                {
                    case State.Terminated:
                        return false;
                    case State.Stopped:
                    case State.Uninitialized:
                        {
                            var action = _runtimeRequests.Take();
                            action(this);
                        }
                        return true;
                    case State.Running running:
                        {
                            _cachedStacktrace = null;
                            var runtimeState = _runtime.Step(running.IgnoreBreak);
                            switch (runtimeState)
                            {
                                case RTE.State.Running:
                                    if (running.SingleStep)
                                    {
                                        Stop(new StoppedEvent(StoppedEvent.ReasonValue.Step)
                                        {
                                            AllThreadsStopped = true,
                                            ThreadId = 0,
                                        });
                                    }
                                    _state = new State.Running(false, false);
                                    break;
                                case RTE.State.EndOfProgram:
                                    _runtime.Call(_entryPoint);
                                    break;
                                case RTE.State.Breakpoint:
                                    Stop(new StoppedEvent(StoppedEvent.ReasonValue.Breakpoint)
                                    {
                                        AllThreadsStopped = true,
                                        ThreadId = 0,
                                    });
                                    break;
                                case RTE.State.Panic panic:
                                    Stop(new StoppedEvent(StoppedEvent.ReasonValue.Exception)
                                    {
                                        AllThreadsStopped = true,
                                        ThreadId = 0,
                                        Text = panic.Exception.Message,
                                    });
                                    break;
                                default:
                                    throw new InvalidOperationException();
                            }
                        }
                        return true;
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        private void Send(Action<DebugRuntime> action)
        {
            _runtimeRequests.Add(action);
        }
        private Task<T> SendCompute<T>(Func<DebugRuntime, T> func)
        {
            var tcs = new TaskCompletionSource<T>();
            Send(dbg =>
            {
                try
                {
                    var result = func(dbg);
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            return tcs.Task;
        }

        public void SendNewBreakpoints(List<(PouId, int)> breakpointLocations) => Send(dbg => dbg._runtime.SetBreakpoints(breakpointLocations));
        public void SendLaunch() => Send(dbg => dbg.Launch());
        public void SendContinue() => Send(dbg => dbg.Continue(ImmutableArray<(PouId, int)>.Empty));
        public void SendStep(ImmutableArray<(PouId, int)> nextLocations) => Send(dbg => dbg.Continue(nextLocations));
        public void SendStepIn(ImmutableArray<(PouId, int)> nextLocations, int frameId, PouId? callee) => Send(dbg => dbg.StepIn(nextLocations, frameId, callee));
        public void SendTerminate() => Send(dbg => dbg.Terminate());
        public void SendPause() => Send(dbg => dbg.Pause());
        public void SendStepSingle() => Send(dbg => dbg.StepSingle());

        public Task<ImmutableArray<StackFrame>> SendGetStacktrace() => SendCompute(dbg => _cachedStacktrace ??= dbg._runtime.GetStackTrace());
        public Task<ImmutableArray<string>> SendGetVariableValues(ImmutableArray<(MemoryLocation Location, IRuntimeType DebugType)?> variables) => SendCompute(dbg =>
        {
            return ImmutableArray.CreateRange(variables, variable =>
            {
                try
                {
                    if (variable is (MemoryLocation Location, IRuntimeType DebugType))
                        return DebugType.ReadValue(Location, _runtime);
                    else
                        return "";
                }
                catch (Exception e)
                {
                    return $"Exception: {e.Message}";
                }
            });
        });
        public CompiledPou? TryGetCompiledPou(string path)
        {
            var normalizedPath = NormalizePath(path);
            foreach (var pou in Module.Pous)
            {
                if (pou.OriginalPath != null && normalizedPath == NormalizePath(pou.OriginalPath))
                {
                    // TODO: Check if the file still matches, otherwise continue.
                    return pou;
                }
            }
            return null;
            static string NormalizePath(string input) => input.Replace('\\', '/').ToLowerInvariant();
        }
    }
}
