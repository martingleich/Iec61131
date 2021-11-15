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
		public static readonly SystemScope SystemScope = new ();
		public static readonly RootScope RootScope = new (SystemScope);

		public sealed class TestProject
		{
			public readonly Project MyProject;
			public TestProject(Project myProject)
			{
				MyProject = myProject ?? throw new ArgumentNullException(nameof(myProject));
			}

			public TestProject AddDutFast(string name, string source)
				=> new (MyProject.Add(new DutLanguageSource($"TYPE {name} : {source}; END_TYPE")));
			public TestProject AddDut(string source)
				=> new (MyProject.Add(new DutLanguageSource(source)));
			public TestProject AddPou(string itf, string body)
				=> new (MyProject.Add(new TopLevelInterfaceAndBodyPouLanguageSource(itf, body)));
			public TestProject AddGVL(string name, string source)
				=> new (MyProject.Add(new GlobalVariableListLanguageSource(name.ToCaseInsensitive(), source)));

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
				var boundFunctionPous = MyProject.LazyBoundModule.Value.FunctionPous.ToImmutableDictionary(x => x.Key.Name, x => x.Value.LazyBoundBody.Value);
				var boundFBPous = MyProject.LazyBoundModule.Value.FunctionBlockPous.ToImmutableDictionary(x => x.Key.Name, x => x.Value.LazyBoundBody.Value);
				var boundPous = boundFunctionPous.Concat(boundFBPous).ToImmutableDictionary(x => x.Key, x => x.Value);
				ExactlyMessages(checks)(boundPous.Values.SelectMany(x => x.Item2));
				return new (boundPous);
			}

			public IBoundExpression BindGlobalExpression(string expression, string? targetType, params Action<IMessage>[] checks)
				=> BindGlobalExpression<IBoundExpression>(expression, targetType, checks);
			public T BindGlobalExpression<T>(string expression, string? targetType, params Action<IMessage>[] checks) where T:IBoundExpression
				=> new TestGlobalExpression(this).BindGlobalExpression<T>(expression, targetType, checks);
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

			public T BindGlobalExpression<T>(string expression, string? targetTypeText, params Action<IMessage>[] checks) where T:IBoundExpression
			{
				var expressionParseMessages = new MessageBag();
				var expressionSyntax = Parser.ParseExpression(expression, expressionParseMessages);
				ExactlyMessages()(expressionParseMessages);
				ExactlyMessages()(Project.MyProject.LazyParseMessages.Value);
				ExactlyMessages()(Project.MyProject.LazyBoundModule.Value.InterfaceMessages);
				var boundModuleInterface = Project.MyProject.LazyBoundModule.Value.Interface;
				var moduleScope = new GlobalModuleScope(boundModuleInterface, RootScope);
				var variables = Variables.ToSymbolSet(x => new LocalVariableSymbol(default, x.Key, MapType(moduleScope, x.Value)));
				var realScope = new VariableSetScope(variables, moduleScope);
				var bindMessages = new MessageBag();
				var targetType = targetTypeText != null ? MapType(moduleScope, targetTypeText) : null;
				var boundExpression = ExpressionBinder.Bind(expressionSyntax, realScope, bindMessages, targetType);
				ExactlyMessages(checks)(bindMessages.ToImmutable());
				return Assert.IsAssignableFrom<T>(boundExpression);
			}
			public IBoundExpression BindGlobalExpression(string expression, string? targetTypeText, params Action<IMessage>[] checks)
				=> BindGlobalExpression<IBoundExpression>(expression, targetTypeText, checks);
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
		public static void CheckVariable(IVariableSymbol var, string name, IType type)
		{
			Assert.Equal(name.ToCaseInsensitive(), var.Name);
			EqualType(type, var.Type);
		}
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
			ConstantExpressionEvaluator.EvaluateConstant(systemScope, expression, bag);
			ExactlyMessages(ErrorOfType<NotAConstantMessage>())(bag);
		}
		public static void HasConstantValue(IBoundExpression expression, SystemScope systemScope, Action<ILiteralValue?> checker)
		{
			var bag = new MessageBag();
			var actualValue = ConstantExpressionEvaluator.EvaluateConstant(systemScope, expression, bag);
			ExactlyMessages()(bag);
			checker(actualValue);
		}

		public static T AssertNthStatement<T>(IBoundStatement statement, int n) where T : IBoundStatement
			=> Assert.IsType<T>(Assert.IsType<SequenceBoundStatement>(statement).Statements[n]);
		public static void AssertStatementBlockMarker(IBoundStatement block, string varName)
			=> AssertVariableExpression(
				AssertNthStatement<ExpressionBoundStatement>(block, 0).Expression,
				varName);
		public static void AssertVariableExpression(IBoundExpression expression, string varName)
			=> Assert.Equal(varName.ToCaseInsensitive(), Assert.IsType<VariableBoundExpression>(expression).Variable.Name);
	}
}
