#nullable enable
using Compiler;
using Compiler.Messages;
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

			public IBoundExpression BindGlobalExpression(string expression, IType? targetType, params Action<IMessage>[] checks)
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

			public IBoundExpression BindGlobalExpression(string expression, IType? targetType, params Action<IMessage>[] checks)
			{
				var expressionParseMessages = new MessageBag();
				var expressionSyntax = Parser.ParseExpression(expression, expressionParseMessages);
				ExactlyMessages()(expressionParseMessages);
				ExactlyMessages()(Project.MyProject.LazyParseMessages.Value);
				ExactlyMessages()(Project.MyProject.LazyBoundModule.Value.InterfaceMessages);
				var boundModuleInterface = Project.MyProject.LazyBoundModule.Value.Interface;
				var moduleScope = new GlobalModuleScope(boundModuleInterface, RootScope.Instance);
				var messageBag = new MessageBag();
				var variables = Variables.ToSymbolSet(x => new LocalVariableSymbol(x.Key, default, TypeCompiler.MapComplete(moduleScope, x.Value, messageBag)));
				Assert.Empty(messageBag);
				var realScope = new VariablesScope(variables, moduleScope);
				var bindMessages = new MessageBag();
				var boundExpression = ExpressionBinder.Bind(expressionSyntax, realScope, bindMessages, targetType);
				ExactlyMessages(checks)(bindMessages.ToImmutable());
				return boundExpression;
			}

			private sealed class VariablesScope : AInnerScope
			{
				public VariablesScope(SymbolSet<LocalVariableSymbol> variables, IScope outerScope) : base(outerScope)
				{
					Variables = variables;
				}
				public SymbolSet<LocalVariableSymbol> Variables { get; }

				public override ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourcePosition sourcePosition)
					=> Variables.TryGetValue(identifier, out var value)
					? ErrorsAnd.Create<IVariableSymbol>(value)
					: base.LookupVariable(identifier, sourcePosition);
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
			Assert.Equal(expected.Code, passed.Code);
		}
	}
}
