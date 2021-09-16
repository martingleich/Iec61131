using Compiler;
using Compiler.Messages;
using System;

namespace Tests
{
	using static ErrorTestHelper;

	public static class BindHelper
	{
		public sealed class TestProject
		{
			private readonly Project MyProject;
			public TestProject(Project myProject)
			{
				MyProject = myProject ?? throw new ArgumentNullException(nameof(myProject));
			}

			public TestProject AddDut(string source)
				=> new (MyProject.Add(new DutLanguageSource(source)));
			public TestProject AddPou(string itf, string body)
				=> new (MyProject.Add(new TopLevelInterfaceAndBodyPouLanguageSource(itf, body)));

			public BoundModuleInterface BindInterfaces(params Action<IMessage>[] checks)
			{
				ExactlyMessages()(MyProject.LazyParseMessages.Value);
				ExactlyMessages(checks)(MyProject.LazyBoundModule.Value.InterfaceMessages);
				return MyProject.LazyBoundModule.Value.Interface;
			}
		}
		
		public static readonly TestProject NewProject = new (Project.Empty);
	}
}
