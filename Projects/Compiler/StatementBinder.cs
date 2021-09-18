using Compiler.Messages;
using System;
using System.Collections.Immutable;

namespace Compiler
{
	public sealed class StatementBinder : IStatementSyntax.IVisitor<IBoundStatement>
	{
		private readonly IScope Scope;
		private readonly MessageBag MessageBag;

		private StatementBinder(IScope scope, MessageBag messageBag)
		{
			Scope = scope ?? throw new ArgumentNullException(nameof(scope));
			MessageBag = messageBag ?? throw new ArgumentNullException(nameof(messageBag));
		}

		public static IBoundStatement Bind(IStatementSyntax syntax, IScope scope, MessageBag messageBag)
		{
			var binder = new StatementBinder(scope, messageBag);
			return syntax.Accept(binder);
		}

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
			var leftSide = ExpressionBinder.Bind(assignStatementSyntax.Left, Scope, MessageBag, null);
			var rightSide = ExpressionBinder.Bind(assignStatementSyntax.Right, Scope, MessageBag, leftSide.Type);
			if (leftSide is not VariableBoundExpression)
				MessageBag.Add(new CannotAssignToSyntaxMessage(assignStatementSyntax.Left.SourcePosition));
			return new AssignBoundStatement(leftSide, rightSide);
		}

		public IBoundStatement Visit(ExpressionStatementSyntax expressionStatementSyntax)
		{
			var boundExpression = ExpressionBinder.Bind(expressionStatementSyntax.Expression, Scope, MessageBag, null);
			return new ExpressionBoundStatement(boundExpression);
		}

		public IBoundStatement Visit(IfStatementSyntax ifStatementSyntax)
		{
			throw new NotImplementedException();
		}

		public IBoundStatement Visit(ReturnStatementSyntax returnStatementSyntax)
		{
			throw new NotImplementedException();
		}

		public IBoundStatement Visit(ExitStatementSyntax exitStatementSyntax)
		{
			throw new NotImplementedException();
		}

		public IBoundStatement Visit(ContinueStatementSyntax continueStatementSyntax)
		{
			throw new NotImplementedException();
		}

		public IBoundStatement Visit(WhileStatementSyntax whileStatementSyntax)
		{
			throw new NotImplementedException();
		}

		public IBoundStatement Visit(ForStatementSyntax forStatementSyntax)
		{
			throw new NotImplementedException();
		}

		public IBoundStatement Visit(EmptyStatementSyntax emptyStatementSyntax)
			=> new SequenceBoundStatement(ImmutableArray<IBoundStatement>.Empty);
	}
}
