using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;
using Xunit.Abstractions;
using Xunit.Runners;

namespace TestPerformance
{
	public static class Program
	{
		class MessageReceiver : IMessageSinkWithTypes
		{
			public void Dispose()
			{
				throw new NotImplementedException();
			}

			public bool OnMessageWithTypes(IMessageSinkMessage message, HashSet<string> messageTypes)
			{
				if (message is IDiscoveryCompleteMessage)
					Discovery.Release();
				if (message is ITestAssemblyFinished)
					if(TestsFinished.CurrentCount < 1)
						TestsFinished.Release();
				return true;
			}
			public SemaphoreSlim Discovery = new SemaphoreSlim(1, 1);
			public SemaphoreSlim TestsFinished = new SemaphoreSlim(1, 1);
		}

		static void Main(string[] args)
		{
			var assemblyName = typeof(Tests.BindHelper).Assembly.Location;

			var msgReceiver = new MessageReceiver();
			var controller = new XunitFrontController(AppDomainSupport.Denied, assemblyName, null, true, null, null, MessageSinkAdapter.Wrap(msgReceiver));

			ITestFrameworkDiscoveryOptions testFrameworkDiscoveryOptions = TestFrameworkOptions.ForDiscovery(null);
			testFrameworkDiscoveryOptions.SetSynchronousMessageReporting(true);
			testFrameworkDiscoveryOptions.SetPreEnumerateTheories(true);
			controller.Find(false, msgReceiver, testFrameworkDiscoveryOptions);

			ITestFrameworkExecutionOptions testFrameworkExecutionOptions = TestFrameworkOptions.ForExecution(null);
			testFrameworkExecutionOptions.SetSynchronousMessageReporting(true);
			testFrameworkExecutionOptions.SetDisableParallelization(true);

			msgReceiver.Discovery.Wait();

			while (true)
			{
				controller.RunAll(msgReceiver, testFrameworkDiscoveryOptions, testFrameworkExecutionOptions);
				msgReceiver.TestsFinished.Wait();
			}
		}
	}
}
