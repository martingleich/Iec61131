#nullable enable
using Compiler;
using Compiler.Messages;
using Compiler.Scopes;
using Compiler.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Tests
{
	using static ErrorHelper;

	public static class BindHelper
	{
		public static readonly SystemScope SystemScope = new("Test".ToCaseInsensitive());

		public sealed class TestProject
		{
			private readonly int IdCounter;
			public readonly Project MyProject;
			public TestProject(int idCounter, Project myProject)
			{
				IdCounter = idCounter;
				MyProject = myProject ?? throw new ArgumentNullException(nameof(myProject));
			}

			public TestProject AddDut(string name, string source)
				=> new(IdCounter, MyProject.Add(new DutLanguageSource($"{MyProject.Name}/{name}", $"TYPE {name} : {source}; END_TYPE")));
			public TestProject AddPou(string itf, string body)
				=> new(IdCounter + 1, MyProject.Add(new TopLevelInterfaceAndBodyPouLanguageSource($"{MyProject.Name}/{IdCounter}", itf, body)));
			public TestProject AddFunction(string name, string itf, string body)
				=> new(IdCounter, MyProject.Add(new TopLevelInterfaceAndBodyPouLanguageSource($"{MyProject.Name}/{name}", $"FUNCTION {name} {itf}", body)));
			public TestProject AddFunctionBlock(string name, string itf, string body)
				=> new(IdCounter, MyProject.Add(new TopLevelInterfaceAndBodyPouLanguageSource($"{MyProject.Name}/{name}", $"FUNCTION_BLOCK {name} {itf}", body)));
			public TestProject AddGVL(string name, string source)
				=> new(IdCounter, MyProject.Add(new GlobalVariableListLanguageSource($"{MyProject.Name}/{name}", name.ToCaseInsensitive(), source)));
			public TestProject AddLibrary(BoundModuleInterface @interface)
				=> new(IdCounter, MyProject.Add(new LibraryLanguageSource(
					SourcePoint.FromOffset($"{MyProject.Name}/Libraries/{@interface.Name}", 0).WithLength(0),
					@interface)));

			public BoundModuleInterface BindInterfaces(params Action<IMessage>[] checks)
			{
				ExactlyMessages()(MyProject.LazyParseMessages.Value);
				ExactlyMessages(checks)(MyProject.LazyBoundModule.Value.InterfaceMessages);
				return MyProject.LazyBoundModule.Value.Interface;
			}

			public TestBoundBodies BindBodies(params Action<IMessage>[] checks)
			{
				ExactlyMessages()(MyProject.LazyParseMessages.Value);
				var boundModule = MyProject.LazyBoundModule.Value;
				var boundPous1 = boundModule.FunctionPous.Select(x => KeyValuePair.Create((ISymbol)x.Key, x.Value));
				var boundPous2 = boundModule.FunctionBlockPous.Select(x => KeyValuePair.Create((ISymbol)x.Key, x.Value));
				var boundPous = Enumerable.Concat(boundPous1, boundPous2).ToImmutableDictionary();
				var bindMessages = boundModule.InterfaceMessages.Concat(boundPous.Values.SelectMany(p => p.LazyBoundBody.Value.Errors));
				ExactlyMessages(checks)(bindMessages);
				return new(boundModule, boundPous);
			}

			public IBoundExpression BindGlobalExpression(string expression, string? targetType, params Action<IMessage>[] checks)
				=> BindGlobalExpression<IBoundExpression>(expression, targetType, checks);
			public T BindGlobalExpression<T>(string expression, string? targetType, params Action<IMessage>[] checks) where T : IBoundExpression
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

			public T BindGlobalExpression<T>(string expression, string? targetTypeText, params Action<IMessage>[] checks) where T : IBoundExpression
			{
				var expressionParseMessages = new MessageBag();
				var expressionSyntax = Parser.ParseExpression("BindGlobalExpression/expression", expression, expressionParseMessages);
				ExactlyMessages()(expressionParseMessages);
				ExactlyMessages()(Project.MyProject.LazyParseMessages.Value);
				ExactlyMessages()(Project.MyProject.LazyBoundModule.Value.InterfaceMessages);
				var boundModuleInterface = Project.MyProject.LazyBoundModule.Value.Interface;
				var moduleScope = new GlobalInternalModuleScope(boundModuleInterface, new RootScope(boundModuleInterface.SystemScope));
				var variables = Variables.ToSymbolSet(x =>
				{
					var type = MapType(moduleScope, x.Value);
					return new LocalVariableSymbol(default, x.Key, type, null);
				});
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
			public readonly BoundModule BoundModule;
			public readonly ImmutableDictionary<ISymbol, BoundPou> BoundPous;
			public TestBoundBodies(BoundModule boundModule, ImmutableDictionary<ISymbol, BoundPou> boundPous)
			{
				BoundModule = boundModule;
				BoundPous = boundPous;
			}

			public KeyValuePair<ISymbol, BoundPou> this[CaseInsensitiveString name] => BoundPous.First(s => s.Key.Name == name);

			public TestBoundBodies Inspect(string name, Action<IBoundStatement> check) => Inspect(name.ToCaseInsensitive(), check);
			public TestBoundBodies Inspect(CaseInsensitiveString name, Action<IBoundStatement> check)
			{
				check(this[name].Value.LazyBoundBody.Value.Value);
				return this;
			}
			public TestBoundBodies InspectFlowMessages(string name, params Action<IMessage>[] checks)
			{
				var entry = this[name.ToCaseInsensitive()];
				ExactlyMessages(checks)(entry.Value.LazyFlowAnalyis.Value);
				return this;
			}
		}

		public static readonly TestProject NewProject = NewNamedProject("Test");
		public static TestProject NewNamedProject(string name) => new(0, Project.Empty(name.ToCaseInsensitive()));

		public static Action<object> OfType<T>(Action<T> action) => x => action(Assert.IsType<T>(x));
		public static Action<InitializerBoundExpression.ABoundElement> ArrayElement(int index, Action<IBoundExpression> expression) => elem =>
		{
			var indexElem = Assert.IsType<InitializerBoundExpression.ABoundElement.ArrayElement>(elem);
			Assert.Equal(index, indexElem.Index.Value);
			expression(indexElem.Value);
		};
		public static Action<InitializerBoundExpression.ABoundElement> FieldElement(string fieldName, Action<IBoundExpression> expression) => OfType<InitializerBoundExpression.ABoundElement.FieldElement>(elem =>
		{
			AssertEx.EqualCaseInsensitive(fieldName, elem.Field.Name);
			expression(elem.Value);
		});
		public static Action<InitializerBoundExpression.ABoundElement> AllElements(Action<IBoundExpression> expression) => elem =>
		{
			var allElements = Assert.IsType<InitializerBoundExpression.ABoundElement.AllElements>(elem);
			expression(allElements.Value);
		};

		public static Action<IBoundExpression> BoundIntLiteral(short value) => expr =>
		{
			var litExpr = Assert.IsType<LiteralBoundExpression>(expr);
			var litValue = Assert.IsType<IntLiteralValue>(litExpr.Value);
			Assert.Equal(value, litValue.Value);
		};
		public static Action<IBoundExpression> BoundBoolLiteral(bool value) => OfType<LiteralBoundExpression>(expr =>
		{
			var litValue = Assert.IsType<BooleanLiteralValue>(expr.Value);
			Assert.Equal(value, litValue.Value);
		});
		public static Action<IBoundExpression> BoundVariable(string name) => expr =>
		{
			var varExpr = Assert.IsType<VariableBoundExpression>(expr);
			AssertEx.EqualCaseInsensitive(name, varExpr.Variable.Name);
		};
	}

	public static class AssertEx
	{
		public static void EqualCaseInsensitive(string expected, CaseInsensitiveString input)
			=> Assert.Equal(expected.ToCaseInsensitive(), input);
		public static void EqualCaseInsensitive(string expected, string input)
			=> Assert.Equal(expected.ToCaseInsensitive(), input.ToCaseInsensitive());
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
