using Compiler.Messages;
using Compiler.Scopes;
using Compiler.Types;
using System;
using System.Collections.Immutable;

namespace Compiler
{
	public sealed class StatementBinder : IStatementSyntax.IVisitor<IBoundStatement>
	{
		private readonly IStatementScope Scope;
		private readonly MessageBag MessageBag;

		private StatementBinder(IStatementScope scope, MessageBag messageBag)
		{
			Scope = scope ?? throw new ArgumentNullException(nameof(scope));
			MessageBag = messageBag ?? throw new ArgumentNullException(nameof(messageBag));
		}

		public static IBoundStatement Bind(IStatementSyntax syntax, IScope scope, MessageBag messageBag)
		{
			var statementScope = new RootStatementScope(scope);
			return BindInner(syntax, statementScope, messageBag);
		}
		private static IBoundStatement BindInner(IStatementSyntax syntax, IStatementScope scope, MessageBag messageBag)
		{
			var binder = new StatementBinder(scope, messageBag);
			return syntax.Accept(binder);
		}
		private IBoundStatement BindInner(IStatementSyntax syntax) => BindInner(syntax, Scope, MessageBag);

		private IBoundExpression BindExpressionWithTargetType(IExpressionSyntax syntax, IType expectedType)
			=> ExpressionBinder.Bind(syntax, Scope, MessageBag, expectedType);
		private IBoundExpression BindExpression(IExpressionSyntax syntax)
			=> ExpressionBinder.Bind(syntax, Scope, MessageBag, null);

		IBoundStatement IStatementSyntax.IVisitor<IBoundStatement>.Visit(StatementListSyntax statementListSyntax)
		{
			var boundStatements = ImmutableArray.CreateBuilder<IBoundStatement>();
			foreach(var statement in statementListSyntax.Statements)
			{
				var boundStatement = statement.Accept(this);
				boundStatements.Add(boundStatement);
			}
			return new SequenceBoundStatement(statementListSyntax, boundStatements.ToImmutable());
		}

		IBoundStatement IStatementSyntax.IVisitor<IBoundStatement>.Visit(AssignStatementSyntax assignStatementSyntax)
		{
			var leftSide = BindExpression(assignStatementSyntax.Left);
			var rightSide = BindExpressionWithTargetType(assignStatementSyntax.Right, leftSide.Type);
			IsLValueChecker.IsLValue(leftSide).Extract(MessageBag);
			return new AssignBoundStatement(assignStatementSyntax, leftSide, rightSide);
		}

		IBoundStatement IStatementSyntax.IVisitor<IBoundStatement>.Visit(ExpressionStatementSyntax expressionStatementSyntax)
		{
			var boundExpression = BindExpression(expressionStatementSyntax.Expression);
			return new ExpressionBoundStatement(expressionStatementSyntax, boundExpression);
		}

		IBoundStatement IStatementSyntax.IVisitor<IBoundStatement>.Visit(IfStatementSyntax ifStatementSyntax)
		{
			IfBoundStatement.Branch BindIfBranch(IfBranchSyntax syntax)
			{
				var boundCondition = BindExpressionWithTargetType(syntax.Condition, Scope.SystemScope.Bool);
				var boundBody = BindInner(syntax.Statements);
				return new(boundCondition, boundBody);
			}
			IfBoundStatement.Branch BindElseIfBranch(ElsifBranchSyntax syntax)
			{
				var boundCondition = BindExpressionWithTargetType(syntax.Condition, Scope.SystemScope.Bool);
				var boundBody = BindInner(syntax.Statements);
				return new(boundCondition, boundBody);
			}
			IfBoundStatement.Branch BindElseBranch(ElseBranchSyntax syntax)
			{
				var boundBody = BindInner(syntax.Statements);
				return new(null, boundBody);
			}

			var branches = ImmutableArray.CreateBuilder<IfBoundStatement.Branch>();
			branches.Add(BindIfBranch(ifStatementSyntax.IfBranch));
			foreach (var elsIfBranch in ifStatementSyntax.ElsIfBranches)
				branches.Add(BindElseIfBranch(elsIfBranch));
			if (ifStatementSyntax.ElseBranch != null)
				branches.Add(BindElseBranch(ifStatementSyntax.ElseBranch));
			return new IfBoundStatement(ifStatementSyntax, branches.ToImmutable());
		}

		IBoundStatement IStatementSyntax.IVisitor<IBoundStatement>.Visit(ReturnStatementSyntax returnStatementSyntax)
			=> new ReturnBoundStatement(returnStatementSyntax);

		IBoundStatement IStatementSyntax.IVisitor<IBoundStatement>.Visit(ExitStatementSyntax exitStatementSyntax)
		{
			if (Scope.InsideLoop)
			{
				return new ExitBoundStatement(exitStatementSyntax);
			}
			else
			{
				MessageBag.Add(new SyntaxOnlyAllowedInLoopMessage(exitStatementSyntax.SourceSpan));
				return new SequenceBoundStatement(exitStatementSyntax, ImmutableArray<IBoundStatement>.Empty);
			}
		}

		IBoundStatement IStatementSyntax.IVisitor<IBoundStatement>.Visit(ContinueStatementSyntax continueStatementSyntax)
		{
			if (Scope.InsideLoop)
			{
				return new ContinueBoundStatement(continueStatementSyntax);
			}
			else
			{
				MessageBag.Add(new SyntaxOnlyAllowedInLoopMessage(continueStatementSyntax.SourceSpan));
				return new SequenceBoundStatement(continueStatementSyntax, ImmutableArray<IBoundStatement>.Empty);
			}
		}

		IBoundStatement IStatementSyntax.IVisitor<IBoundStatement>.Visit(WhileStatementSyntax whileStatementSyntax)
		{
			var boundCondition = BindExpressionWithTargetType(whileStatementSyntax.Condition, Scope.SystemScope.Bool);
			var bodyScope = new LoopScope(Scope);
			var boundBody = BindInner(whileStatementSyntax.Statements, bodyScope, MessageBag);
			return new WhileBoundStatement(whileStatementSyntax, condition: boundCondition, body: boundBody);
		}

		IBoundStatement IStatementSyntax.IVisitor<IBoundStatement>.Visit(ForStatementSyntax forStatementSyntax)
		{
			var boundIndex = BindExpression(forStatementSyntax.IndexVariable);
			IsLValueChecker.IsLValue(boundIndex).Extract(MessageBag);
			var boundInitial = BindExpressionWithTargetType(forStatementSyntax.InitialValue, boundIndex.Type);
			var boundUpperBound = BindExpressionWithTargetType(forStatementSyntax.UpperBound, boundIndex.Type);
			IBoundExpression boundStep;
			if (forStatementSyntax.ByClause is not null)
				boundStep = BindExpressionWithTargetType(forStatementSyntax.ByClause.StepSize, boundIndex.Type);
			else
				boundStep = BindExpressionWithTargetType(new LiteralExpressionSyntax(new IntegerLiteralToken(OverflowingInteger.FromLong(1), "", default, default)), boundIndex.Type);

			var realIndexType = TypeRelations.ResolveAlias(boundIndex.Type);
			FunctionVariableSymbol incrementFunctionSymbol;
			if (realIndexType is BuiltInType builtInType && Scope.SystemScope.BuiltInFunctionTable.TryGetOperatorFunction(("ADD", true), builtInType) is OperatorFunction incrementOperatorFunction)
			{
				incrementFunctionSymbol = incrementOperatorFunction.Symbol;
			}
			else
			{
				MessageBag.Add(new CannotUseTypeAsLoopIndexMessage(realIndexType, forStatementSyntax.IndexVariable.SourceSpan));
				var errorName = ImplicitName.ErrorBinaryOperator(realIndexType.Code, realIndexType.Code, "ADD");
				incrementFunctionSymbol = FunctionVariableSymbol.CreateError(forStatementSyntax.SourceSpan, errorName, realIndexType);
			}

			var bodyScope = new LoopScope(Scope);
			var boundBody = BindInner(forStatementSyntax.Statements, bodyScope, MessageBag);

			return new ForLoopBoundStatement(
				forStatementSyntax,
				boundIndex,
				boundInitial,
				boundUpperBound,
				boundStep,
				incrementFunctionSymbol,
				boundBody);
		}

		IBoundStatement IStatementSyntax.IVisitor<IBoundStatement>.Visit(EmptyStatementSyntax emptyStatementSyntax)
			=> new SequenceBoundStatement(emptyStatementSyntax, ImmutableArray<IBoundStatement>.Empty);
	}
}
