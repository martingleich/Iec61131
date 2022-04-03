using System;
using System.Collections.Concurrent;
using System.IO;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;

namespace Runtime
{
	public sealed class DebugAdapter : DebugAdapterBase
	{
		private readonly TextWriter _logger;
		private readonly Runtime _runtime;
		private readonly BlockingCollection<Action> _runtimeRequests = new();
		private bool _launch;
		private bool _terminate;

		private DebugAdapter(
			Stream streamIn,
			Stream streamOut,
			Runtime runtime,
			TextWriter logger)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

			InitializeProtocolClient(streamIn, streamOut);
		}

		protected override ResponseBody HandleProtocolRequest(string requestType, object requestArgs)
		{
			_logger.WriteLine($"Request: {requestType}");
			return base.HandleProtocolRequest(requestType, requestArgs);
		}
		protected override void HandleProtocolError(Exception ex)
		{
			_logger.WriteLine($"Error: {ex}");
			base.HandleProtocolError(ex);
		}

		protected override InitializeResponse HandleInitializeRequest(InitializeArguments arguments) => new ()
			{
			};
		protected override LaunchResponse HandleLaunchRequest(LaunchArguments arguments)
		{
			Send(() => _launch = true);
			return new LaunchResponse();
		}
		protected override TerminateResponse HandleTerminateRequest(TerminateArguments arguments)
		{
			Send(() => _terminate = true);
			return new TerminateResponse();
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

			Protocol.SendEvent(new ExitedEvent());
			Protocol.WaitForReader();
		}

		public static void Run(
			Stream streamIn,
			Stream streamOut,
			Runtime runtime,
			TextWriter logger)
		{
			var debugAdapter = new DebugAdapter(streamIn, streamOut, runtime, logger);
			debugAdapter.Run();
		}
	}
}
