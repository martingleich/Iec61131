using System;
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

namespace Runtime
{
	public sealed class DebugAdapter : DebugAdapterBase
	{
		private sealed class DebugRuntime
		{
			private bool _launch;
			private bool _terminate;
			private bool _stop;
			private readonly List<Range<int>> _newTempBreakpoints = new();

			private readonly DebugAdapter _adapter;
			private readonly Runtime _runtime;
			private readonly BlockingCollection<Action<DebugRuntime>> _runtimeRequests = new();

			public DebugRuntime(Runtime runtime, DebugAdapter adapter)
			{
				_runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
				_adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
			}

			public void Run()
			{
				// Wait for the initials command
				while (true)
				{
					var action = _runtimeRequests.Take();
					action(this);
					if (this._launch)
					{
						this._launch = false;
						break;
					}
					if (this._terminate)
					{
						this._terminate = false;
						break;
					}
				}

				// We are read to do things
				_adapter.Protocol.SendEvent(new InitializedEvent());

				// Cycle until termination.
				bool isRunning = true;
				while (true)
				{
					_runtime.Reset();
					while (true)
					{
						if (isRunning)
						{
							var state = _runtime.Step();
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
						if (_runtimeRequests.TryTake(out var action))
						{
							action(this);

							if (this._terminate)
							{
								this._terminate = false;
								goto TERMINATE;
							}
							if (this._stop)
							{
								this._stop = false;
								isRunning = false;
							}
							if (this._launch)
							{
								this._launch = false;
								isRunning = true;
							}
							if (this._newTempBreakpoints.Count > 0)
							{
								_runtime.SetTemporaryBreakpoints(this._newTempBreakpoints);
								this._newTempBreakpoints.Clear();
							}
						}
					}
				}
				TERMINATE:

				_adapter.Protocol.SendEvent(new ExitedEvent(0));
			}
			public void Send(Action<DebugRuntime> action)
			{
				_runtimeRequests.Add(action);
			}
			public void SendNewTemporaryBreakpoints(List<Range<int>> breakpointLocations) => Send(dbg => dbg._newTempBreakpoints.AddRange(breakpointLocations));
			public void SendLaunch() => Send(dbg => dbg._launch = true);
			public void SendTerminate() => Send(dbg => dbg._terminate = true);
			public void SendStop() => Send(dbg => dbg._stop = true);
			public Task<ImmutableArray<(CompiledPou Cpou, int CurAddress)>> GetStacktrace()
			{
				var tcs = new TaskCompletionSource<ImmutableArray<(CompiledPou, int)>>();
				Send(dbg =>
				{
					try
					{
						var stacktrace = dbg._runtime.GetStackTrace();
						tcs.SetResult(stacktrace);
					}
					catch (Exception e)
					{
						tcs.SetException(e);
					}
				});
				return tcs.Task;
			}
		}

		private readonly ILogger _logger;
		private readonly DebugRuntime _debugRuntime;

		private readonly ImmutableArray<CompiledPou> _allPous;

		private DebugAdapter(
			Stream streamIn,
			Stream streamOut,
			Runtime runtime,
			ILogger logger,
			ImmutableArray<CompiledPou> allPous)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_debugRuntime = new DebugRuntime(runtime, this);
			_allPous = allPous;

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

		protected override InitializeResponse HandleInitializeRequest(InitializeArguments arguments) => new()
		{
			ExceptionBreakpointFilters = new List<ExceptionBreakpointsFilter>(),
			SupportsExceptionFilterOptions = false,
			SupportsExceptionOptions = false,
			SupportsTerminateThreadsRequest = false,
		};
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
		private static string NormalizePath(string input) => input.Replace('\\', '/').ToLowerInvariant();
		private CompiledPou? TryGetCompiledPou(string path)
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
		}

		protected override SetBreakpointsResponse HandleSetBreakpointsRequest(SetBreakpointsArguments arguments)
		{
			// Step 1: Find the matching debug data based on the path?
			var pou = TryGetCompiledPou(arguments.Source.Path);
			if (pou == null || pou.BreakpointMap == null)
			{
				return new SetBreakpointsResponse(arguments.Breakpoints
					.Select(req => new Breakpoint(false)
					{
						Message = $"Could not find debug data for '{arguments.Source.Path}'."
					})
					.ToList());
			}

			// Step 3: Find a breakpoints.
			List<Breakpoint> result = new List<Breakpoint>();
			List<Range<int>> breakpointLocations = new List<Range<int>>();
			foreach (var requestBreakpoint in arguments.Breakpoints)
			{
				int line = requestBreakpoint.Line;
				int? collumn = requestBreakpoint.Column;
				var breakpoint = pou.BreakpointMap.TryGetBreakpointBySource(line, collumn);
				if (breakpoint != null)
				{
					result.Add(new Breakpoint(true)
					{
						Line = breakpoint.StartLine,
						Column = breakpoint.StartCollumn,
						EndLine = breakpoint.EndLine - 1,
						EndColumn = breakpoint.EndCollumn,
						Source = arguments.Source,
					});
					breakpointLocations.Add(new Range<int>(breakpoint.StartInstruction, breakpoint.EndInstruction));
				}
				else
				{
					result.Add(new Breakpoint(false)
					{
						Message = $"No breakpoint at this source position"
					});
				}
			}
			_debugRuntime.SendNewTemporaryBreakpoints(breakpointLocations);
			return new SetBreakpointsResponse(result);
		}

		protected override StackTraceResponse HandleStackTraceRequest(StackTraceArguments arguments)
		{
			var trace = _debugRuntime.GetStacktrace().GetAwaiter().GetResult();
			var frames = trace.Select((frame, id) =>
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
					return new StackFrame(id, compiledPou.Id.Name, 0, 0)
					{
						Source = source,
					};
				}
				else
				{
					return new StackFrame(id, compiledPou.Id.Name, curPosition.StartLine, curPosition.StartCollumn)
					{
						EndLine = curPosition.EndLine,
						EndColumn = curPosition.EndCollumn,
						Source = source
					};
				}
			}).ToList();
			return new StackTraceResponse(frames);
		}

		protected override ScopesResponse HandleScopesRequest(ScopesArguments arguments)
		{
			// TODO: Return variables here.
			// Scope for the arguments of the current pou.
			// Scope for the currently readable local variables of the current pou.
			//new Scope(
			return new ScopesResponse();
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
