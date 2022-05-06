using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using Runtime.IR;
using Runtime.IR.RuntimeTypes;

namespace Runtime
{
    public sealed class DebugRuntime
    {
        private enum State
        {
            Uninitialized,
            Running,
            Stopped,
            Terminated,
        }

        private readonly DebugAdapter _adapter;
        private readonly Runtime _runtime;

        private readonly BlockingCollection<Action<DebugRuntime>> _runtimeRequests = new();

        private readonly ImmutableArray<CompiledPou> _allPous;
        private readonly ImmutableArray<CompiledGlobalVariableList> _allGvls;
        private readonly PouId _entryPoint;

        public ImmutableArray<CompiledGlobalVariableList> AllGvls => _allGvls;

        private ImmutableArray<StackFrame>? _cachedStacktrace;

        private State _state = State.Uninitialized;

        // Part of state Running
        private bool _ignoreBreak = true;
        private bool _singleStep = true;

        public DebugRuntime(
            DebugAdapter adapter,
            Runtime runtime,
            ImmutableArray<CompiledPou> allPous,
            ImmutableArray<CompiledGlobalVariableList> allGvls,
            PouId entryPoint)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _allPous = allPous;
            _allGvls = allGvls;
            _entryPoint = entryPoint;
        }

        private void Launch()
        {
            if (_state == State.Uninitialized)
            {
                _state = State.Running;
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
            if (_state == State.Running)
            {
                _state = State.Stopped;
                _adapter.Protocol.SendEvent(reason);
            }
        }
        private void Terminate()
        {
            _state = State.Uninitialized;
            _adapter.Protocol.SendEvent(new ExitedEvent());
        }
        private void Continue(ImmutableArray<(PouId, int)> nextLocations)
        {
            _runtime.SetTemporaryBreakpoints(nextLocations);
            _ignoreBreak = true;
            _state = State.Running;
        }
        private void StepIn(ImmutableArray<(PouId, int)> nextLocations, int frameId, PouId? callee)
        {
            _runtime.SetTemporaryStepInBreakpoint(nextLocations, frameId, callee);
            _ignoreBreak = true;
            _state = State.Running;
        }
        private void StepSingle()
        {
            _singleStep = true;
            _ignoreBreak = true;
            _state = State.Running;
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
                    case State.Running:
                        {
                            _cachedStacktrace = null;
                            var runtimeState = _runtime.Step(_ignoreBreak);
                            _ignoreBreak = true;
                            switch (runtimeState)
                            {
                                case Runtime.State.Running:
                                    if (_singleStep)
                                    {
                                        _singleStep = false;
                                        Stop(new StoppedEvent(StoppedEvent.ReasonValue.Step)
                                        {
                                            AllThreadsStopped = true,
                                            ThreadId = 0,
                                        });
                                    }
                                    break;
                                case Runtime.State.EndOfProgram:
                                    _runtime.Call(_entryPoint);
                                    break;
                                case Runtime.State.Breakpoint:
                                    Stop(new StoppedEvent(StoppedEvent.ReasonValue.Breakpoint)
                                    {
                                        AllThreadsStopped = true,
                                        ThreadId = 0,
                                    });
                                    break;
                                case Runtime.State.Panic panic:
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
        public Task<ImmutableArray<string>> SendGetStackVariableValues(int frameId, ImmutableArray<(LocalVarOffset Offset, IRuntimeType DebugType)> variables) => SendCompute(dbg =>
        {
            return ImmutableArray.CreateRange(variables, variable =>
            {
                try
                {
                    var location = dbg._runtime.LoadEffectiveAddress(frameId, variable.Offset);
                    return variable.DebugType.ReadValue(location, _runtime);
                }
                catch (Exception e)
                {
                    return $"Exception: {e.Message}";
                }
            });
        });
        public Task<ImmutableArray<string>> SendGetVariableValues(ImmutableArray<(MemoryLocation Location, IRuntimeType DebugType)> variables) => SendCompute(dbg =>
        {
            return ImmutableArray.CreateRange(variables, variable =>
            {
                try
                {
                    return variable.DebugType.ReadValue(variable.Location, _runtime);
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
            foreach (var pou in _allPous)
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
