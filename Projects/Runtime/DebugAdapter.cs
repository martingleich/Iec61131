﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using Runtime.IR;
using Runtime.IR.RuntimeTypes;
using static Runtime.Runtime;

namespace Runtime
{
    public sealed class DebugRuntime
    {
        private bool _launch;
        private bool _terminate;
        private bool _pause;
        private bool _ignoreBreak;
        private bool _singleStep;

        private readonly DebugAdapter _adapter;
        private readonly Runtime _runtime;

        private readonly BlockingCollection<Action<DebugRuntime>> _runtimeRequests = new();

        private readonly ImmutableArray<CompiledPou> _allPous;

        private ImmutableArray<StackFrame>? _cachedStackframe;

        public DebugRuntime(DebugAdapter adapter, Runtime runtime, ImmutableArray<CompiledPou> allPous)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _allPous = allPous;
        }

        public void Run()
        {
            WaitForInitialLaunch();
            _adapter.Protocol.SendEvent(new InitializedEvent());
            CycleMain();
            _adapter.Protocol.SendEvent(new ExitedEvent(0));

            void WaitForInitialLaunch()
            {
                while (true)
                {
                    var action = _runtimeRequests.Take();
                    action(this);
                    if (_launch)
                    {
                        _launch = false;
                        break;
                    }
                    if (_terminate)
                    {
                        _terminate = false;
                        break;
                    }
                }
            }

            void CycleMain()
            {
                bool isCycling = true;
                while (isCycling)
                {
                    _runtime.Reset();
                    bool isRunning = true;
                    while (isCycling)
                    {
                        Action<DebugRuntime>? nextAction;
                        if (isRunning)
                        {
                            try
                            {
                                _cachedStackframe = null;
                                State state = _runtime.Step(_ignoreBreak);
                                _ignoreBreak = false;
                                if (_singleStep)
                                {
                                    _singleStep = false;
                                    isRunning = false;
                                }
                                if (state == Runtime.State.EndOfProgram)
                                    break;
                                if (state == Runtime.State.Breakpoint)
                                {
                                    isRunning = false;
                                    _adapter.Protocol.SendEvent(new StoppedEvent(StoppedEvent.ReasonValue.Breakpoint)
                                    {
                                        AllThreadsStopped = true,
                                        ThreadId = 0,
                                    });
                                }
                            }
                            catch (PanicException pe)
                            {
                                isRunning = false;
                                _adapter.Protocol.SendEvent(new StoppedEvent(StoppedEvent.ReasonValue.Exception)
                                {
                                    AllThreadsStopped = true,
                                    ThreadId = 0,
                                    Text = pe.Message,
                                });
                            }
                            nextAction = _runtimeRequests.TryTake(out var na) ? na : null;
                        }
                        else
                        {
                            nextAction = _runtimeRequests.Take();
                        }

                        if (nextAction != null)
                        {
                            nextAction(this);

                            if (_terminate)
                            {
                                _terminate = false;
                                isCycling = false;
                            }
                            if (_pause)
                            {
                                _pause = false;
                                _adapter.Protocol.SendEvent(new StoppedEvent(StoppedEvent.ReasonValue.Pause)
                                {
                                    AllThreadsStopped = true,
                                    ThreadId = 0
                                });
                                isRunning = false;
                            }
                            if (_launch)
                            {
                                _launch = false;
                                isRunning = true;
                            }
                        }
                    }
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
        public void SendLaunch() => Send(dbg => dbg._launch = true);
        public void SendContinue() => Send(dbg => { dbg._launch = true; dbg._ignoreBreak = true; });
        public void SendStep(ImmutableArray<(PouId, int)> nextLocations) => Send(dbg => { dbg._launch = true; dbg._ignoreBreak = true; dbg._runtime.SetTemporaryBreakpoints(nextLocations); });
        public void SendStepIn(ImmutableArray<(PouId, int)> nextLocations, int frameId, PouId? callee) => Send(dbg => { dbg._launch = true; dbg._ignoreBreak = true; dbg._runtime.SetTemporaryStepInBreakpoint(nextLocations, frameId, callee); });
        public void SendTerminate() => Send(dbg => dbg._terminate = true);
        public void SendPause() => Send(dbg => dbg._pause = true);
        public void SendStep() => Send(dbg => { dbg._launch = true; dbg._ignoreBreak = true; dbg._singleStep = true; });

        public Task<ImmutableArray<StackFrame>> GetStacktrace() => SendCompute(dbg => _cachedStackframe ??= dbg._runtime.GetStackTrace());
        public Task<ImmutableArray<string>> GetVariableValues(int frameId, ImmutableArray<(LocalVarOffset Offset, IRuntimeType DebugType)> variables) => SendCompute(dbg =>
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

    public sealed class DebugAdapter : DebugAdapterBase
    {
        private readonly ILogger _logger;
        private readonly DebugRuntime _debugRuntime;

        private DebugAdapter(
            Stream streamIn,
            Stream streamOut,
            Runtime runtime,
            ILogger logger,
            ImmutableArray<CompiledPou> allPous)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _debugRuntime = new DebugRuntime(this, runtime, allPous);

            InitializeProtocolClient(streamIn, streamOut);
        }

        protected override ResponseBody HandleProtocolRequest(string requestType, object requestArgs)
        {
            _logger.LogTrace($"Request: {requestType}");
            return base.HandleProtocolRequest(requestType, requestArgs);
        }
        protected override void HandleProtocolError(Exception ex)
        {
            _logger.LogCritical(ex, "HandleProtocolError");
            base.HandleProtocolError(ex);
        }
        private InitializeArguments? _initializeArguments = null;
        protected override InitializeResponse HandleInitializeRequest(InitializeArguments arguments)
        {
            _initializeArguments = arguments;
            return new()
            {
                ExceptionBreakpointFilters = new List<ExceptionBreakpointsFilter>(),
                SupportsExceptionFilterOptions = false,
                SupportsExceptionOptions = false,
                SupportsTerminateThreadsRequest = false,
                SupportsStepInTargetsRequest = true,
            };
        }
        protected override LaunchResponse HandleLaunchRequest(LaunchArguments arguments)
        {
            _debugRuntime.SendLaunch();
            return new LaunchResponse();
        }
        protected override TerminateResponse HandleTerminateRequest(TerminateArguments arguments)
        {
            if (arguments.Restart == true)
                throw new NotSupportedException();
            _debugRuntime.SendTerminate();
            return new TerminateResponse();
        }
        protected override DisconnectResponse HandleDisconnectRequest(DisconnectArguments arguments)
        {
            if (arguments.Restart == true || arguments.ResumableDisconnect == true || arguments.SuspendDebuggee == true || arguments.TerminateDebuggee == false)
                throw new NotSupportedException();
            _debugRuntime.SendTerminate();
            return new DisconnectResponse();
        }
        protected override SetExceptionBreakpointsResponse HandleSetExceptionBreakpointsRequest(SetExceptionBreakpointsArguments arguments)
        {
            // No-op: Not supported.
            return new SetExceptionBreakpointsResponse();
        }
        protected override ThreadsResponse HandleThreadsRequest(ThreadsArguments arguments)
        {
            return new(new List<Thread>()
            {
                new Thread(0, "MainTask")
            });
        }
        protected override SetBreakpointsResponse HandleSetBreakpointsRequest(SetBreakpointsArguments arguments)
        {
            // Step 1: Find the matching debug data based on the path?
            var pou = _debugRuntime.TryGetCompiledPou(arguments.Source.Path);
            if (pou == null || pou.BreakpointMap == null)
            {
                return new SetBreakpointsResponse(arguments.Breakpoints
                    .Select(req => new Breakpoint(false)
                    {
                        Message = $"Could not find debug data for '{arguments.Source.Path}'."
                    })
                    .ToList());
            }

            // Step 2: Find the breakpoints.
            var result = new List<Breakpoint>();
            var breakpointLocations = new List<(PouId, int)>();
            foreach (var requestBreakpoint in arguments.Breakpoints)
            {
                var breakpoint = pou.BreakpointMap.TryGetBreakpointBySource(requestBreakpoint.Line, requestBreakpoint.Column);
                if (breakpoint != null)
                {
                    result.Add(new Breakpoint(true)
                    {
                        Line = breakpoint.Txt.Start.Line,
                        Column = breakpoint.Txt.Start.Collumn,
                        EndLine = breakpoint.Txt.End.Line,
                        EndColumn = breakpoint.Txt.End.Collumn,
                        Source = arguments.Source,
                    });
                    breakpointLocations.Add((pou.Id, breakpoint.Instruction.Start));
                }
                else
                {
                    result.Add(new Breakpoint(false)
                    {
                        Message = $"No breakpoint at this source position"
                    });
                }
            }
            _debugRuntime.SendNewBreakpoints(breakpointLocations);
            return new SetBreakpointsResponse(result);
        }
        protected override StackTraceResponse HandleStackTraceRequest(StackTraceArguments arguments)
        {
            var trace = _debugRuntime.GetStacktrace().GetAwaiter().GetResult();
            var frames = trace.Select(ConvertStackFrame).ToList();
            frames.Reverse();
            return new StackTraceResponse(frames)
            {
                TotalFrames = frames.Count,
            };

            static Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages.StackFrame ConvertStackFrame(StackFrame frame)
            {
                var compiledPou = frame.Cpou;
                var source = new Source()
                {
                    Name = compiledPou.Id.Name,
                    Path = compiledPou.OriginalPath,
                };
                var curAddress = frame.CurAddress;
                var curPosition = compiledPou.BreakpointMap?.FindBreakpointByInstruction(curAddress);
                if (curPosition == null)
                {
                    return new(frame.FrameId, compiledPou.Id.Name, 0, 0)
                    {
                        Source = source,
                    };
                }
                else
                {
                    return new(frame.FrameId, compiledPou.Id.Name, curPosition.Txt.Start.Line, curPosition.Txt.Start.Collumn)
                    {
                        EndLine = curPosition.Txt.End.Line,
                        EndColumn = curPosition.Txt.End.Collumn,
                        Source = source
                    };
                }
            }
        }
        protected override ScopesResponse HandleScopesRequest(ScopesArguments arguments)
        {
            var curCallstack = _debugRuntime.GetStacktrace().GetAwaiter().GetResult();
            if (!curCallstack.IsDefault && arguments.FrameId >= 0 && arguments.FrameId < curCallstack.Length)
            {
                var frame = curCallstack[arguments.FrameId];
                var argScope = new Scope("Arguments", arguments.FrameId * 2, false)
                {
                    PresentationHint = Scope.PresentationHintValue.Arguments,
                    NamedVariables = frame.Cpou.VariableTable?.CountArgs
                };
                var localScope = new Scope("Locals", arguments.FrameId * 2 + 1, false)
                {
                    PresentationHint = Scope.PresentationHintValue.Locals,
                    NamedVariables = frame.Cpou.VariableTable?.CountLocals
                };
                return new ScopesResponse(new List<Scope>()
                {
                    argScope,
                    localScope
                });
            }
            else
            {
                return new ScopesResponse();
            }
        }
        protected override VariablesResponse HandleVariablesRequest(VariablesArguments arguments)
        {
            var curCallstack = _debugRuntime.GetStacktrace().GetAwaiter().GetResult();
            var varRef = arguments.VariablesReference;
            var frame = curCallstack[varRef / 2];
            var args = varRef % 2 == 0;
            ImmutableArray<VariableTable.StackVariable> requests;
            if (frame.Cpou.VariableTable != null)
            {
                IEnumerable<VariableTable.StackVariable> variables;
                variables = args ? frame.Cpou.VariableTable.Args : frame.Cpou.VariableTable.Locals;
                if (arguments.Start is int startVar)
                    variables = variables.Skip(startVar);
                if (arguments.Count is int varCount)
                    variables = variables.Take(varCount);
                requests = variables.ToImmutableArray();
            }
            else
            {
                requests = ImmutableArray<VariableTable.StackVariable>.Empty;
            }

            var valueRequests = requests.Select(v => (v.StackOffset, v.Type)).ToImmutableArray();
            var values = _debugRuntime.GetVariableValues(frame.FrameId, valueRequests).GetAwaiter().GetResult();
            var resultVariables = new List<Variable>();
            for (int i = 0; i < requests.Length; i++)
            {
                var resultVar = new Variable(requests[i].Name, values[i], 0)
                {
                    EvaluateName = requests[i].Name,
                    Type = _initializeArguments?.SupportsVariableType == true ? requests[i].Type.Name : null,
                };
                resultVariables.Add(resultVar);
            }
            return new VariablesResponse(resultVariables);
        }
        protected override ContinueResponse HandleContinueRequest(ContinueArguments arguments)
        {
            _debugRuntime.SendContinue();
            return new ContinueResponse() { AllThreadsContinued = arguments.ThreadId != 0 };
        }
        protected override PauseResponse HandlePauseRequest(PauseArguments arguments)
        {
            _debugRuntime.SendPause();
            return new PauseResponse();
        }

        protected override StepInTargetsResponse HandleStepInTargetsRequest(StepInTargetsArguments arguments)
        {
            var curCallstack = _debugRuntime.GetStacktrace().GetAwaiter().GetResult();
            var curFrame = curCallstack[arguments.FrameId];
            var curBreakpoint = curFrame.Cpou.BreakpointMap?.FindBreakpointByInstruction(curFrame.CurAddress);
            if (curBreakpoint != null)
            {
                var code = curFrame.Cpou.Code;
                List<int> stepInIds = new List<int>();
                Stack<int> cursors = new Stack<int>();
                HashSet<int> visisited = new HashSet<int>();
                cursors.Push(curFrame.CurAddress);
                while (cursors.TryPop(out int cursor))
                {
                    if (curBreakpoint.Instruction.Contains(cursor) && visisited.Add(cursor))
                    {
                        if (code[cursor] is IR.Statements.StaticCall)
                            stepInIds.Add(cursor);
                        else if (code[cursor] is IR.Statements.Jump jump)
                            cursors.Push(jump.Target.StatementId);
                        else if (code[cursor] is IR.Statements.JumpIfNot jumpIfNot)
                            cursors.Push(jumpIfNot.Target.StatementId);
                        cursors.Push(cursor + 1);
                    }
                }
                var stepIns = stepInIds.Select(stepIn => new StepInTarget(stepIn, ((IR.Statements.StaticCall)code[stepIn]).Callee.Name)).ToList();
                return new StepInTargetsResponse(stepIns);
            }
            return base.HandleStepInTargetsRequest(arguments);
        }
        protected override StepInResponse HandleStepInRequest(StepInArguments arguments)
        {
            var curCallstack = _debugRuntime.GetStacktrace().GetAwaiter().GetResult();
            var curFrame = curCallstack[^1];
            var curBreakpoint = curFrame.Cpou.BreakpointMap?.FindBreakpointByInstruction(curFrame.CurAddress);
            if (curBreakpoint != null)
            {
                PouId? target = arguments.TargetId is int targetId ? ((IR.Statements.StaticCall)curFrame.Cpou.Code[targetId]).Callee : null;
                var nextStatementPositions = curBreakpoint.Successors.Select(b => (curFrame.Cpou.Id, b.Instruction.Start)).ToImmutableArray();
                _debugRuntime.SendStepIn(nextStatementPositions, curFrame.FrameId + 1, target);
            }
            return new StepInResponse();
        }
        protected override NextResponse HandleNextRequest(NextArguments arguments)
        {
            switch (arguments.Granularity)
            {
                case SteppingGranularity.Instruction:
                    _debugRuntime.SendStep();
                    break;
                default: // Unsupported values default to statement
                    {
                        var curCallstack = _debugRuntime.GetStacktrace().GetAwaiter().GetResult();
                        var curFrame = curCallstack[^1];
                        var curBreakpoint = curFrame.Cpou.BreakpointMap?.FindBreakpointByInstruction(curFrame.CurAddress);
                        if (curBreakpoint != null)
                        {
                            IEnumerable<BreakpointMap.Breakpoint> nextBreakpoints;
                            if (arguments.Granularity == SteppingGranularity.Line)
                                nextBreakpoints = curBreakpoint.NextLineBreakpoints;
                            else
                                nextBreakpoints = curBreakpoint.Successors;
                            var nextStatementPositions = nextBreakpoints.Select(b => (curFrame.Cpou.Id, b.Instruction.Start)).ToImmutableArray();
                            _debugRuntime.SendStep(nextStatementPositions);
                        }
                    }
                    break;
            }
            return new NextResponse();
        }
        protected override StepOutResponse HandleStepOutRequest(StepOutArguments arguments)
        {
            _debugRuntime.SendStep(ImmutableArray<(PouId, int)>.Empty);
            return new StepOutResponse();
        }

        private void Run()
        {
            Protocol.Run();
            _debugRuntime.Run();
            Protocol.WaitForReader();
        }

        public static void Run(
            Stream streamIn,
            Stream streamOut,
            Runtime runtime,
            ILogger logger,
            ImmutableArray<CompiledPou> allPous)
        {
            var debugAdapter = new DebugAdapter(streamIn, streamOut, runtime, logger, allPous);
            debugAdapter.Run();
        }
    }
}
