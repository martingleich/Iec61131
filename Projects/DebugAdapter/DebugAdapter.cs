using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Compiler;
using Compiler.CodegenIR;
using Compiler.Scopes;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using Runtime.IR;
using Runtime.IR.RuntimeTypes;

namespace DebugAdapter
{
    using RTE = Runtime.RTE;
    using IR = Runtime.IR;
    public sealed class DebugAdapter : DebugAdapterBase
    {
        private readonly ILogger _logger;
        private readonly DebugRuntime _debugRuntime;

        private DebugAdapter(
            Stream streamIn,
            Stream streamOut,
            RTE runtime,
            ILogger logger,
            CompiledModule module,
            PouId entryPoint)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _debugRuntime = new DebugRuntime(this, runtime, module, entryPoint);

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
            Protocol.SendEvent(new OutputEvent(ex.ToString())
            {
                Category = OutputEvent.CategoryValue.MessageBox,
                Severity = OutputEvent.SeverityValue.Error,
            });
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
                SupportsSetExpression = false,
                SupportsSetVariable = true,
                SupportsValueFormattingOptions = false,
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
        private VariableReferenceManager? _varReferenceManager;
        protected override StackTraceResponse HandleStackTraceRequest(StackTraceArguments arguments)
        {
            var trace = _debugRuntime.SendGetStacktrace().GetAwaiter().GetResult();
            var frames = trace.Select(ConvertStackFrame).ToList();
            frames.Reverse();
            _varReferenceManager = new VariableReferenceManager();
            return new StackTraceResponse(frames)
            {
                TotalFrames = frames.Count,
            };

            static StackFrame ConvertStackFrame(Runtime.StackFrame frame)
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
            var curCallstack = _debugRuntime.SendGetStacktrace().GetAwaiter().GetResult();
            if (!curCallstack.IsDefault && arguments.FrameId >= 0 && arguments.FrameId < curCallstack.Length && _varReferenceManager != null)
            {
                var frame = curCallstack[arguments.FrameId];
                var globalsRef = _varReferenceManager.GetGlobal(_debugRuntime.Module.GlobalVariableLists);
                var argsRef = _varReferenceManager.GetStack(arguments.FrameId, true, frame.BaseAddress, frame.Cpou.VariableTable?.Args ?? Enumerable.Empty<VariableTable.StackVariable>());
                var localsRef = _varReferenceManager.GetStack(arguments.FrameId, false, frame.BaseAddress, frame.Cpou.VariableTable?.Locals ?? Enumerable.Empty<VariableTable.StackVariable>());
                return new ScopesResponse(new List<Scope>()
                {
                    argsRef.GetScope(),
                    localsRef.GetScope(),
                    globalsRef.GetScope(),
                });
            }
            else
            {
                return new ScopesResponse();
            }
        }

        protected override VariablesResponse HandleVariablesRequest(VariablesArguments arguments)
        {
            if (_varReferenceManager == null)
                return new VariablesResponse();

            var varRef = _varReferenceManager.Get(arguments.VariablesReference);
            var children = varRef.GetChildren().Subrange(arguments.Start, arguments.Count).ToImmutableArray();
            var valueRequests = children.Select(child => child.ValueRequest).ToImmutableArray();
            var values = _debugRuntime.SendGetVariableValues(valueRequests).GetAwaiter().GetResult();
            var variables = children.Zip(values, (child, value) =>
                new Variable(child.Name, value, child.ChildCount > 0 ? child.Id.Value : 0)
                {
                    EvaluateName = child.Path,
                    Type = _initializeArguments?.SupportsVariableType == true ? child.Type?.Name : null,
                    IndexedVariables = child.ChildCount,
                })
                .ToList();

            return new VariablesResponse(variables);
        }
        protected override EvaluateResponse HandleEvaluateRequest(EvaluateArguments arguments)
        {
            return new EvaluateResponse("Failed to evaluate expression.", 0);
        }
        protected override SetVariableResponse HandleSetVariableRequest(SetVariableArguments arguments)
        {
            if (_varReferenceManager == null)
                return new SetVariableResponse();

            var containerVarRef = _varReferenceManager.Get(arguments.VariablesReference);
            if (containerVarRef.TryGetChildByName(arguments.Name) is not VariableReference varRef)
                return new SetVariableResponse($"There is no variable named '{arguments.Name}' in '{containerVarRef.Name}'.");
            if(varRef.ValueRequest is not (MemoryLocation, IRuntimeType) valueRequest)
                return new SetVariableResponse($"Cannot write to variable '{arguments.Name}'");
            var rootScope = GetScopeOf(_debugRuntime.Module);
            var compilerType = GetCompilerType(rootScope, valueRequest.Item2);
            if(compilerType == null)
                return new SetVariableResponse($"Unsupported type '{valueRequest.Item2.Name}'.");
            var messageBag = new Compiler.Messages.MessageBag();
            var expression = Parser.ParseExpression("value", arguments.Value, messageBag);
            var boundExpression = ExpressionBinder.Bind(expression, rootScope, messageBag, compilerType);
            if (messageBag.HasError)
                return new SetVariableResponse($"Error: {string.Join(Environment.NewLine, messageBag)}");
            var runtimeTypeFactory = GetRuntimeTypeFactoryOf(_debugRuntime.Module);
            var assigner = CodegenIR.GenerateAssignment(runtimeTypeFactory, new PouId("$anonymous_setvariable"), valueRequest.Item1, boundExpression);
            var result = _debugRuntime.SendExecute(assigner).GetAwaiter().GetResult();
            if(result != null)
                return new SetVariableResponse($"{result.Exception.Message}");
            var newValue = _debugRuntime.SendGetVariableValues(ImmutableArray.Create(varRef.ValueRequest)).GetAwaiter().GetResult()[0];
            return new SetVariableResponse(newValue);
        }

        private static IScope GetScopeOf(CompiledModule module)
        {
            var systemScope = new SystemScope(CaseInsensitiveString.Empty);
            var rootScope = new RootScope(systemScope);
            return rootScope;
        }
        private static RuntimeTypeFactoryFromType GetRuntimeTypeFactoryOf(CompiledModule module)
        {
            return new RuntimeTypeFactoryFromType(module.Types.OfType<RuntimeTypeStructured>());
        }

        private static Compiler.Types.IType? GetCompilerType(IScope scope, IRuntimeType runtimeType)
        {
            foreach (var builtIn in scope.SystemScope.BuiltInTypeTable.AllBuiltInTypes)
            {
                if (builtIn.Name.Original == runtimeType.Name)
                    return builtIn;
            }
            return null;
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
            var curCallstack = _debugRuntime.SendGetStacktrace().GetAwaiter().GetResult();
            var curFrame = curCallstack[arguments.FrameId];
            var cpou = curFrame.Cpou;
            var instruction = curFrame.CurAddress;
            var curBreakpoint = cpou.BreakpointMap?.FindBreakpointByInstruction(instruction);
            if (curBreakpoint != null)
            {
                var code = cpou.Code;
                var stepInIds = new List<int>();
                var cursors = new Stack<int>();
                var visisited = new HashSet<int>();
                cursors.Push(instruction);
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
            var curCallstack = _debugRuntime.SendGetStacktrace().GetAwaiter().GetResult();
            var curFrame = curCallstack[^1];
            var curBreakpoint = curFrame.Cpou.BreakpointMap?.FindBreakpointByInstruction(curFrame.CurAddress);
            if (curBreakpoint != null)
            {
                PouId? target = arguments.TargetId is int targetId ? ((IR.Statements.StaticCall)curFrame.Cpou.Code[targetId]).Callee : null;
                var nextStatementPositions = curBreakpoint.Successors.Select(b => (curFrame.Cpou.Id, b.Instruction.Start)).ToImmutableArray();
                _debugRuntime.SendStepToFrame(nextStatementPositions, curFrame.FrameId + 1, target);
            }
            return new StepInResponse();
        }
        protected override NextResponse HandleNextRequest(NextArguments arguments)
        {
            switch (arguments.Granularity)
            {
                case SteppingGranularity.Instruction:
                    _debugRuntime.SendStepSingle();
                    break;
                default: // Unsupported values default to statement
                    {
                        var curCallstack = _debugRuntime.SendGetStacktrace().GetAwaiter().GetResult();
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
            var curCallstack = _debugRuntime.SendGetStacktrace().GetAwaiter().GetResult();
            _debugRuntime.SendStepToFrame(ImmutableArray<(PouId, int)>.Empty, curCallstack[^2].FrameId, null);
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
            RTE runtime,
            ILogger logger,
            CompiledModule module,
            PouId entryPoint)
        {
            var debugAdapter = new DebugAdapter(streamIn, streamOut, runtime, logger, module, entryPoint);
            debugAdapter.Run();
        }
    }
}
