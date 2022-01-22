using Compiler.Messages;
using Compiler.Scopes;
using Compiler.Types;
using System;
using System.Collections.Immutable;

namespace Compiler
{
	public sealed class StatementBinder : IStatementSyntax.IVisitor<IBoundStatement>
	{
		private readonly InlineVarDeclTreeNode VarDeclNode;
		private readonly IStatementScope Scope;
		private readonly MessageBag MessageBag;

		private StatementBinder(IStatementScope scope, InlineVarDeclTreeNode varDeclNode, MessageBag messageBag)
		{
			Scope = scope ?? throw new ArgumentNullException(nameof(scope));
			MessageBag = messageBag ?? throw new ArgumentNullException(nameof(messageBag));
			VarDeclNode = varDeclNode ?? throw new ArgumentNullException(nameof(varDeclNode));
		}

		private static IBoundStatement BindInner(
			StatementListSyntax syntax,
			VarDeclTreeNode outerVarDeclNode,
			IStatementScope outerScope,
			MessageBag messageBag)
		{
			var innerVarDeclNode = outerVarDeclNode.AddChild(syntax);
			var innerScope = innerVarDeclNode.GetScope(outerScope);
			var binder = new StatementBinder(innerScope, innerVarDeclNode, messageBag);
			return binder.Visit(syntax);
		}
		private IBoundStatement BindInner(StatementListSyntax syntax) => BindInner(
			syntax,
			VarDeclNode,
			Scope,
			MessageBag);

		public static IBoundStatement Bind(
			StatementListSyntax syntax,
			VarDeclTreeNode outerVarDeclNode,
			IScope outerScope,
			MessageBag messageBag)
		{
			var statementScope = new RootStatementScope(outerScope);
			return BindInner(syntax, outerVarDeclNode, statementScope, messageBag);
		}

		private IBoundExpression BindExpressionWithTargetType(IExpressionSyntax syntax, IType expectedType)
			=> ExpressionBinder.Bind(syntax, Scope, MessageBag, expectedType);
		private IBoundExpression BindExpression(IExpressionSyntax syntax)
			=> ExpressionBinder.Bind(syntax, Scope, MessageBag, null);

		public IBoundStatement Visit(StatementListSyntax statementListSyntax)
		{
			var boundStatements = ImmutableArray.CreateBuilder<IBoundStatement>();
			foreach (var statement in statementListSyntax.Statements)
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
			var boundBody = BindInner(whileStatementSyntax.Statements, VarDeclNode, bodyScope, MessageBag);
			return new WhileBoundStatement(whileStatementSyntax, condition: boundCondition, body: boundBody);
		}

		IBoundStatement IStatementSyntax.IVisitor<IBoundStatement>.Visit(ForStatementSyntax forStatementSyntax)
		{
			IBoundExpression boundIndex;
			IBoundExpression boundInitial;
			InlineVarDeclTreeNode varDeclTreeNode;
			IStatementScope scope;
			if (forStatementSyntax.Index is ForStatementExternalIndexSyntax externalIndex)
			{
				boundIndex = BindExpression(externalIndex.IndexVariable);
				IsLValueChecker.IsLValue(boundIndex).Extract(MessageBag);
				boundInitial = BindExpressionWithTargetType(externalIndex.Initial.Value, boundIndex.Type);
				varDeclTreeNode = VarDeclNode;
				scope = Scope;
			}
			else
			{
				var declaredIndex = (ForStatementDeclareLocalIndexSyntax)forStatementSyntax.Index;
				varDeclTreeNode = VarDeclNode.AddChild(declaredIndex);
				var variable = varDeclTreeNode.GetVarFor(declaredIndex);
				GetLocalVariableArgs(
					declaredIndex.SourceSpan,
					declaredIndex.Type,
					declaredIndex.Initial,
					variable,
					out var type,
					out var initial);
				variable.Declare(type);

				boundIndex = new VariableBoundExpression(declaredIndex, variable);
				boundInitial = initial;
				scope = varDeclTreeNode.GetScope(Scope);
			}

			var boundUpperBound = BindExpressionWithTargetType(forStatementSyntax.UpperBound, boundIndex.Type);
			IBoundExpression boundStep;
			if (forStatementSyntax.ByClause is not null)
				boundStep = BindExpressionWithTargetType(forStatementSyntax.ByClause.StepSize, boundIndex.Type);
			else
				boundStep = BindExpressionWithTargetType(
					new LiteralExpressionSyntax(
						new IntegerLiteralToken(OverflowingInteger.FromLong(1), "", SourcePoint.Null, null)), boundIndex.Type);

			var realIndexType = TypeRelations.ResolveAlias(boundIndex.Type);
			FunctionVariableSymbol incrementFunctionSymbol;
			if (realIndexType is BuiltInType builtInType && scope.SystemScope.BuiltInFunctionTable.TryGetOperatorFunction(("ADD", true), builtInType) is OperatorFunction incrementOperatorFunction)
			{
				incrementFunctionSymbol = incrementOperatorFunction.Symbol;
			}
			else
			{
				MessageBag.Add(new CannotUseTypeAsLoopIndexMessage(realIndexType, forStatementSyntax.Index.SourceSpan));
				var errorName = ImplicitName.ErrorBinaryOperator(realIndexType.Code, realIndexType.Code, "ADD");
				incrementFunctionSymbol = FunctionVariableSymbol.CreateError(forStatementSyntax.SourceSpan, errorName, realIndexType);
			}

			var bodyScope = new LoopScope(scope);
			var boundBody = BindInner(forStatementSyntax.Statements, varDeclTreeNode, bodyScope, MessageBag);

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

		IBoundStatement IStatementSyntax.IVisitor<IBoundStatement>.Visit(LocalVarDeclStatementSyntax localVarDeclStatementSyntax)
		{
			var variable = VarDeclNode.GetVarFor(localVarDeclStatementSyntax);
			GetLocalVariableArgs(
				localVarDeclStatementSyntax.SourceSpan,
				localVarDeclStatementSyntax.Type,
				localVarDeclStatementSyntax.Initial,
				variable,
				out var type,
				out var initial);
			if(!variable.IsDeclared) // This can happen if the same variable is declared multiple times in the same block, just ignore the second declaration.
				variable.Declare(type);
			return new InitVariableBoundStatement(localVarDeclStatementSyntax, variable, initial);
		}
		void GetLocalVariableArgs(
			SourceSpan sourceSpan,
			VarTypeSyntax? typeSyntax,
			VarInitSyntax? initialSyntax,
			IVariableSymbol variable,
			out IType outType,
			[System.Diagnostics.CodeAnalysis.NotNullIfNotNull("initialSyntax")] out IBoundExpression? outBoundExpression)
		{
			if (typeSyntax != null)
			{
				outType = TypeCompiler.MapComplete(Scope, typeSyntax.Type, MessageBag);
				if (initialSyntax != null)
				{
					outBoundExpression = BindExpressionWithTargetType(initialSyntax.Value, outType);
				}
				else
				{
					outBoundExpression = null;
				}
			}
			else if (initialSyntax != null)
			{
				outBoundExpression = BindExpression(initialSyntax.Value);
				outType = outBoundExpression.Type;
			}
			else
			{
				MessageBag.Add(new CannotInferTypeOfVariableMessage(sourceSpan, variable));
				outType = ITypeSymbol.CreateErrorForVar(sourceSpan, variable.Name);
				outBoundExpression = null;
			}
		}
	}
}
