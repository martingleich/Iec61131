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

		public static IBoundStatement Bind(IStatementSyntax syntax, IStatementScope scope, MessageBag messageBag)
		{
			var binder = new StatementBinder(scope, messageBag);
			return syntax.Accept(binder);
		}

		private IBoundExpression BindExpressionWithTargetType(IExpressionSyntax syntax, IType expectedType)
			=> ExpressionBinder.Bind(syntax, Scope, MessageBag, expectedType);
		private IBoundExpression BindExpression(IExpressionSyntax syntax)
			=> ExpressionBinder.Bind(syntax, Scope, MessageBag, null);

		public IBoundStatement Visit(StatementListSyntax statementListSyntax)
		{
			var boundStatements = ImmutableArray.CreateBuilder<IBoundStatement>();
			foreach(var statement in statementListSyntax.Statements)
			{
				var boundStatement = statement.Accept(this);
				boundStatements.Add(boundStatement);
			}
			return new SequenceBoundStatement(boundStatements.ToImmutable());
		}

		public IBoundStatement Visit(AssignStatementSyntax assignStatementSyntax)
		{
			var leftSide = BindExpression(assignStatementSyntax.Left);
			var rightSide = BindExpressionWithTargetType(assignStatementSyntax.Right, leftSide.Type);
			if (leftSide is not VariableBoundExpression)
				MessageBag.Add(new CannotAssignToSyntaxMessage(assignStatementSyntax.Left.SourcePosition));
			return new AssignBoundStatement(leftSide, rightSide);
		}

		public IBoundStatement Visit(ExpressionStatementSyntax expressionStatementSyntax)
		{
			var boundExpression = BindExpression(expressionStatementSyntax.Expression);
			return new ExpressionBoundStatement(boundExpression);
		}

		public IBoundStatement Visit(IfStatementSyntax ifStatementSyntax)
		{
			IfBoundStatement.Branch BindIfBranch(IfBranchSyntax syntax)
			{
				var boundCondition = BindExpressionWithTargetType(syntax.Condition, Scope.SystemScope.Bool);
				var boundBody = Visit(syntax.Statements);
				return new(boundCondition, boundBody);
			}
			IfBoundStatement.Branch BindElseIfBranch(ElsifBranchSyntax syntax)
			{
				var boundCondition = BindExpressionWithTargetType(syntax.Condition, Scope.SystemScope.Bool);
				var boundBody = Visit(syntax.Statements);
				return new(boundCondition, boundBody);
			}
			IfBoundStatement.Branch BindElseBranch(ElseBranchSyntax syntax)
			{
				var boundBody = Visit(syntax.Statements);
				return new(null, boundBody);
			}

			var branches = ImmutableArray.CreateBuilder<IfBoundStatement.Branch>();
			branches.Add(BindIfBranch(ifStatementSyntax.IfBranch));
			foreach (var elsIfBranch in ifStatementSyntax.ElsIfBranches)
				branches.Add(BindElseIfBranch(elsIfBranch));
			if (ifStatementSyntax.ElseBranch != null)
				branches.Add(BindElseBranch(ifStatementSyntax.ElseBranch));
			return new IfBoundStatement(branches.ToImmutable());
		}

		public IBoundStatement Visit(ReturnStatementSyntax returnStatementSyntax)
		{
			throw new NotImplementedException();
		}

		public IBoundStatement Visit(ExitStatementSyntax exitStatementSyntax)
		{
			if (Scope.InsideLoop)
			{
				return new ExitBoundStatement();
			}
			else
			{
				MessageBag.Add(new SyntaxOnlyAllowedInLoopMessage(exitStatementSyntax.SourcePosition));
				return new SequenceBoundStatement(ImmutableArray<IBoundStatement>.Empty);
			}
		}

		public IBoundStatement Visit(ContinueStatementSyntax continueStatementSyntax)
		{
			if (Scope.InsideLoop)
			{
				return new ContinueBoundStatement();
			}
			else
			{
				MessageBag.Add(new SyntaxOnlyAllowedInLoopMessage(continueStatementSyntax.SourcePosition));
				return new SequenceBoundStatement(ImmutableArray<IBoundStatement>.Empty);
			}
		}

		public IBoundStatement Visit(WhileStatementSyntax whileStatementSyntax)
		{
			var boundCondition = BindExpressionWithTargetType(whileStatementSyntax.Condition, Scope.SystemScope.Bool);
			var bodyScope = new LoopScope(Scope);
			var boundBody = Bind(whileStatementSyntax.Statements, bodyScope, MessageBag);
			return new WhileBoundStatement(boundCondition, boundBody);
		}

		public IBoundStatement Visit(ForStatementSyntax forStatementSyntax)
		{
			throw new NotImplementedException();
		}

		public IBoundStatement Visit(EmptyStatementSyntax emptyStatementSyntax)
			=> new SequenceBoundStatement(ImmutableArray<IBoundStatement>.Empty);
	}
}
