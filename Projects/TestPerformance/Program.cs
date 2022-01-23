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
				Messages.Add(message.ToString()!);
				if (message is ITestCaseDiscoveryMessage testCaseDiscoveryMessage)
					TestCases.AddRange(testCaseDiscoveryMessage.TestCases);
				if (message is IDiscoveryCompleteMessage)
					Discovery.Release();
				if (message is ITestAssemblyFinished)
					TestsFinished.Release();
				return true;
			}
			public List<string> Messages = new();
			public SemaphoreSlim Discovery = new SemaphoreSlim(0, 1);
			public List<ITestCase> TestCases = new();
			public SemaphoreSlim TestsFinished = new SemaphoreSlim(0, 1);
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
			msgReceiver.Discovery.Wait();
			var testCases = msgReceiver.TestCases;

			ITestFrameworkExecutionOptions testFrameworkExecutionOptions = TestFrameworkOptions.ForExecution(null);
			testFrameworkExecutionOptions.SetSynchronousMessageReporting(true);
			testFrameworkExecutionOptions.SetDisableParallelization(true);

			while (true)
			{
				controller.RunTests(testCases, msgReceiver, testFrameworkExecutionOptions);
				msgReceiver.TestsFinished.Wait();
			}
		}
	}
}
