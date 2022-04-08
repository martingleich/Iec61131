using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;

namespace Runtime
{
	public sealed class DebugAdapter : DebugAdapterBase
	{
		private readonly ILogger _logger;
		private readonly Runtime _runtime;
		private readonly BlockingCollection<Action> _runtimeRequests = new();
		private bool _launch;
		private bool _terminate;

		private DebugAdapter(
			Stream streamIn,
			Stream streamOut,
			Runtime runtime,
			ILogger logger)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

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
			Send(() => _launch = true);
			return new LaunchResponse();
		}
		protected override TerminateResponse HandleTerminateRequest(TerminateArguments arguments)
		{
			if (arguments.Restart == true)
				throw new NotSupportedException();
			Send(() => _terminate = true);
			return new TerminateResponse();
		}
		protected override DisconnectResponse HandleDisconnectRequest(DisconnectArguments arguments)
		{
			if (arguments.Restart == true || arguments.ResumableDisconnect == true || arguments.SuspendDebuggee == true || arguments.TerminateDebuggee == false)
				throw new NotSupportedException();
			Send(() => _terminate = true);
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

		private void Send(Action action)
		{
			_runtimeRequests.Add(action);
		}

		private void Run()
		{
			// Start the communication thread.
			Protocol.Run();

			// Wait for the initials command
			while (!_launch && !_terminate)
			{
				var action = _runtimeRequests.Take();
				action();
			}

			// We are read to do things
			Protocol.SendEvent(new InitializedEvent());

			// Cycle until termination.
			while (_launch && !_terminate)
			{
				_runtime.Reset();
				while (_runtime.Step())
				{
					if (_runtimeRequests.TryTake(out var action))
						action();
				}
			}

			Protocol.SendEvent(new ExitedEvent(0));
			Protocol.WaitForReader();
		}

		public static void Run(
			Stream streamIn,
			Stream streamOut,
			Runtime runtime,
			ILogger logger)
		{
			var debugAdapter = new DebugAdapter(streamIn, streamOut, runtime, logger);
			debugAdapter.Run();
		}
	}
}
