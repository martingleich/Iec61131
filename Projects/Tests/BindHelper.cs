#nullable enable
using Compiler;
using Compiler.Messages;
using Compiler.Types;
using System;
using Xunit;

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

			public IBoundExpression BindGlobalExpression(string expression, IType? targetType, params Action<IMessage>[] checks)
			{
				var expressionParseMessages = new MessageBag();
				var expressionSyntax = Parser.ParseExpression(expression, expressionParseMessages);
				ExactlyMessages()(expressionParseMessages);
				ExactlyMessages()(MyProject.LazyParseMessages.Value);
				ExactlyMessages()(MyProject.LazyBoundModule.Value.InterfaceMessages);
				var boundModuleInterface = MyProject.LazyBoundModule.Value.Interface;
				var scope = new GlobalModuleScope(boundModuleInterface, RootScope.Instance);
				var bindMessages = new MessageBag();
				var boundExpression = ExpressionBinder.Bind(expressionSyntax, scope, bindMessages, targetType);
				ExactlyMessages(checks)(bindMessages.ToImmutable());
				return boundExpression;
			}

			internal object BindGlobalExpression(string v, object p, object errorOfType)
			{
				throw new NotImplementedException();
			}
		}
		
		public static readonly TestProject NewProject = new (Project.Empty);
	}

	public static class AssertEx
	{
		public static void EqualType(IType expected, IType passed)
		{
			Assert.Equal(expected.Code, passed.Code);
		}
	}
}
