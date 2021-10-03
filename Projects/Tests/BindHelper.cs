#nullable enable
using Compiler;
using Compiler.Messages;
using Compiler.Scopes;
using Compiler.Types;
using System;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Tests
{
	using static ErrorTestHelper;

	public static class BindHelper
	{
		public sealed class TestProject
		{
			public readonly Project MyProject;
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

			public TestBoundBodies BindBodies(params Action<IMessage>[] checks)
			{
				ExactlyMessages()(MyProject.LazyParseMessages.Value);
				ExactlyMessages()(MyProject.LazyBoundModule.Value.InterfaceMessages);
				var boundPous = MyProject.LazyBoundModule.Value.Pous.ToImmutableDictionary(x => x.Key.Name, x => x.Value.LazyBoundBody.Value);
				ExactlyMessages(checks)(boundPous.Values.SelectMany(x => x.Item2));
				return new (boundPous);
			}

			public IBoundExpression BindGlobalExpression(string expression, string? targetType, params Action<IMessage>[] checks)
				=> new TestGlobalExpression(this).BindGlobalExpression(expression, targetType, checks);
			public TestGlobalExpression WithGlobalVar(string name, string type)
				=> new TestGlobalExpression(this).WithGlobalVar(name, type);
		}

		public sealed class TestGlobalExpression
		{
			public readonly TestProject Project;
			public readonly ImmutableDictionary<CaseInsensitiveString, ITypeSyntax> Variables;

			public TestGlobalExpression(TestProject project) : this(project, ImmutableDictionary<CaseInsensitiveString, ITypeSyntax>.Empty)
			{
			}

			public TestGlobalExpression(TestProject project, ImmutableDictionary<CaseInsensitiveString, ITypeSyntax> variables)
			{
				Project = project ?? throw new ArgumentNullException(nameof(project));
				Variables = variables ?? throw new ArgumentNullException(nameof(variables));
			}

			public TestGlobalExpression WithGlobalVar(string name, string type)
			{
				if (name is null)
					throw new ArgumentNullException(nameof(name));
				if (type is null)
					throw new ArgumentNullException(nameof(type));
				var parsed = ParserTestHelper.ParseType(type);
				return new(Project, Variables.Add(name.ToCaseInsensitive(), parsed));
			}

			private static IType MapType(IScope scope, string typeText)
			{
				var typeSyntax = ParserTestHelper.ParseType(typeText);
				return MapType(scope, typeSyntax);
			}
			private static IType MapType(IScope scope, ITypeSyntax typeSyntax)
			{
				var messageBag = new MessageBag();
				var type = TypeCompiler.MapComplete(scope, typeSyntax, messageBag);
				Assert.Empty(messageBag);
				return type;
			}

			public IBoundExpression BindGlobalExpression(string expression, string? targetTypeText, params Action<IMessage>[] checks)
			{
				var expressionParseMessages = new MessageBag();
				var expressionSyntax = Parser.ParseExpression(expression, expressionParseMessages);
				ExactlyMessages()(expressionParseMessages);
				ExactlyMessages()(Project.MyProject.LazyParseMessages.Value);
				ExactlyMessages()(Project.MyProject.LazyBoundModule.Value.InterfaceMessages);
				var boundModuleInterface = Project.MyProject.LazyBoundModule.Value.Interface;
				var moduleScope = new GlobalModuleScope(boundModuleInterface, RootScope.Instance);
				var variables = Variables.ToSymbolSet(x => new LocalVariableSymbol(x.Key, default, MapType(moduleScope, x.Value)));
				var realScope = new VariableSetScope(variables, moduleScope);
				var bindMessages = new MessageBag();
				var targetType = targetTypeText != null ? MapType(moduleScope, targetTypeText) : null;
				var boundExpression = ExpressionBinder.Bind(expressionSyntax, realScope, bindMessages, targetType);
				ExactlyMessages(checks)(bindMessages.ToImmutable());
				return boundExpression;
			}
		}

		public sealed class TestBoundBodies
		{
			private readonly ImmutableDictionary<CaseInsensitiveString, (IBoundStatement, ImmutableArray<IMessage>)> BoundPous;
			public TestBoundBodies(ImmutableDictionary<CaseInsensitiveString, (IBoundStatement, ImmutableArray<IMessage>)> boundPous)
			{
				BoundPous = boundPous;
			}

			public TestBoundBodies Inspect(string name, Action< IBoundStatement> check) => Inspect(name.ToCaseInsensitive(), check);
			public TestBoundBodies Inspect(CaseInsensitiveString name, Action<IBoundStatement> check)
			{
				check(BoundPous[name].Item1);
				return this;
			}
		}
		
		public static readonly TestProject NewProject = new (Project.Empty);

	}

	public static class AssertEx
	{
		public static void EqualType(IType expected, IType passed)
		{
			EqualType(expected.Code, passed);
		}
		public static void EqualType(string typeCode, IType passed)
		{
			Assert.Equal(typeCode.ToCaseInsensitive(), passed.Code.ToCaseInsensitive());
		}
		public static void NotAConstant(IBoundExpression expression, SystemScope systemScope)
		{
			var bag = new MessageBag();
			ConstantExpressionEvaluator.EvaluateConstant(expression, bag, systemScope);
			ExactlyMessages(ErrorOfType<NotAConstantMessage>())(bag);
		}
		public static void HasConstantValue(IBoundExpression expression, SystemScope systemScope, Action<ILiteralValue?> checker)
		{
			var bag = new MessageBag();
			var actualValue = ConstantExpressionEvaluator.EvaluateConstant(expression, bag, systemScope);
			ExactlyMessages()(bag);
			checker(actualValue);
		}
	}
}
