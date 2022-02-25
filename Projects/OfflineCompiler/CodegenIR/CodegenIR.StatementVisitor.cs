using System;
using System.Linq;
using Compiler;
using Compiler.Types;
using StandardLibraryExtensions;
using IR = Runtime.IR;

namespace OfflineCompiler
{
	public sealed partial class CodegenIR
	{
		private sealed class StatementVisitor : IBoundStatement.IVisitor
		{
			private readonly CodegenIR CodeGen;
			private readonly IR.Label? _loopExitLabel;
			private readonly IR.Label? _loopContinueLabel;

			public StatementVisitor(CodegenIR codeGen) : this(codeGen, default, default)
			{
			}
			private StatementVisitor(CodegenIR codeGen, IR.Label? loopExitLabel, IR.Label? loopContinueLabel)
			{
				CodeGen = codeGen ?? throw new ArgumentNullException(nameof(codeGen));
				_loopExitLabel = loopExitLabel;
				_loopContinueLabel = loopContinueLabel;
			}

			private StatementVisitor GetInLoopVisitor(IR.Label loopExitLabel, IR.Label loopContinueLabel) => new (CodeGen, loopExitLabel, loopContinueLabel);

			private void AddComment(IBoundNode boundNode)
			{
				var text = SyntaxToStringConverter.ExactToString(boundNode.OriginalNode);
				CodeGen.Generator.IL_Comment(text.Trim());
			}
			private void AddComment(params INode[] nodes)
			{
				var text = nodes.Select(n => SyntaxToStringConverter.ExactToString(n)).DelimitWith("");
				CodeGen.Generator.IL_Comment(text.Trim());
			}
			private void Assign(IWritable writable, IBoundExpression valueExpression)
			{
				var value = valueExpression.Accept(CodeGen._loadValueExpressionVisitor);
				writable.Assign(CodeGen, value);
			}

			public void Visit(SequenceBoundStatement sequenceBoundStatement)
			{
				foreach (var st in sequenceBoundStatement.Statements)
					st.Accept(this);
			}

			public void Visit(ExpressionBoundStatement expressionBoundStatement)
			{
				AddComment(expressionBoundStatement);
				expressionBoundStatement.Expression.Accept(CodeGen._loadValueExpressionVisitor);
			}

			public void Visit(AssignBoundStatement assignToExpressionBoundStatement)
			{
				AddComment(assignToExpressionBoundStatement);
				var writable = CodeGen.LoadWritable(assignToExpressionBoundStatement.LeftSide);
				Assign(writable, assignToExpressionBoundStatement.RightSide);
			}
			public void Visit(InitVariableBoundStatement initVariableBoundStatement)
			{
				AddComment(initVariableBoundStatement);
				if (initVariableBoundStatement.RightSide is not null)
				{
					var writable = initVariableBoundStatement.LeftSide.Accept(CodeGen._variableAddressableVisitor).ToWritable(CodeGen);
					Assign(writable, initVariableBoundStatement.RightSide);
				}
			}

			public void Visit(IfBoundStatement ifBoundStatement)
			{
				var endLabel = CodeGen.Generator.DeclareLabel();
				for (int i = 0; i < ifBoundStatement.Branches.Length; ++i)
				{
					var branch = ifBoundStatement.Branches[i];
					if (i == ifBoundStatement.Branches.Length - 1)
					{
						if (branch.Condition is IBoundExpression condition)
						{
							AddComment(condition);
							var conditionValue = CodeGen.LoadValueAsVariable(condition);
							CodeGen.Generator.IL_Jump_IfNot(conditionValue, endLabel);
						}
						branch.Body.Accept(this);
						CodeGen.Generator.IL_Label(endLabel);
					}
					else
					{
						var nextLabel = CodeGen.Generator.DeclareLabel();
						if (branch.Condition is IBoundExpression condition)
						{
							AddComment(condition);
							var conditionValue = CodeGen.LoadValueAsVariable(condition);
							CodeGen.Generator.IL_Jump_IfNot(conditionValue, nextLabel);
							branch.Body.Accept(this);
							CodeGen.Generator.IL_Jump(endLabel);
							CodeGen.Generator.IL_Label(nextLabel);
						}
						else
						{
							branch.Body.Accept(this);
							CodeGen.Generator.IL_Label(endLabel);
							break; // Code after this is unreachable.
						}
					}
				}
			}

			public void Visit(ForLoopBoundStatement forLoopBoundStatement)
			{
				var forSyntax = forLoopBoundStatement.OriginalNode as ForStatementSyntax;
				if(forSyntax != null) AddComment(forSyntax.TokenFor, forSyntax.Index);
				var indexPointer = CodeGen.LoadAddressAsVariable(forLoopBoundStatement.Index);
				var initialValue = CodeGen.LoadValueAsVariable(forLoopBoundStatement.Initial);
				if(forSyntax != null) AddComment(forSyntax.TokenTo, forSyntax.UpperBound);
				var upperBound = CodeGen.LoadValueAsVariable(forLoopBoundStatement.UpperBound);
				if(forSyntax?.ByClause != null) AddComment(forSyntax.ByClause);
				var step = CodeGen.LoadValueAsVariable(forLoopBoundStatement.Step);
				// idxPointer := ADR(idx);
				// IF NOT __System.ForLoopInit(initialValue, step, upperBound)
				//    goto END;
				// START:
				// idxPointer* := initialValue;
				// <body>
				// CONTINUE:
				// atEnd := __SYSTEM.ForLoopNext(idxPointer, step, upperBound)
				// if NOT atEnd
				//     goto START;
				// EXIT:
				GenerateForLoop(
					forSyntax,
					forLoopBoundStatement.IndexType.Code,
					st => forLoopBoundStatement.Body.Accept(st),
					indexPointer,
					initialValue,
					upperBound,
					step);
			}

			public void GenerateForLoop(
				ForStatementSyntax? forSyntax,
				string indexTypeCode,
				Action<StatementVisitor> generateBody,
				LocalVariable indexPointer,
				LocalVariable initialValue,
				LocalVariable upperBound,
				LocalVariable step)
			{
				if(forSyntax != null) AddComment(forSyntax.TokenDo);
				var loopStart = CodeGen.Generator.DeclareLabel();
				var loopExit = CodeGen.Generator.DeclareLabel();
				var loopContinue = CodeGen.Generator.DeclareLabel();

				var initResult = CodeGen.Generator.IL_SimpleCallAsVariable(
					IR.Type.Bits8,
					IR.PouId.ForLoopInit(indexTypeCode),
					initialValue,
					step,
					upperBound);
				CodeGen.Generator.IL_Jump_IfNot(initResult, loopExit);
				CodeGen.Generator.IL_WriteDeref(initialValue, indexPointer);
				CodeGen.Generator.IL_Label(loopStart);
				generateBody(GetInLoopVisitor(loopExit, loopContinue));

				if (forSyntax != null) AddComment(forSyntax.TokenEndFor);
				CodeGen.Generator.IL_Label(loopContinue);
				var nextResult = CodeGen.Generator.IL_SimpleCallAsVariable(
					IR.Type.Bits8,
					IR.PouId.ForLoopNext(indexTypeCode),
					indexPointer,
					step,
					upperBound);
				CodeGen.Generator.IL_Jump_IfNot(nextResult, loopStart);
				CodeGen.Generator.IL_Label(loopExit);
			}
			public void Visit(WhileBoundStatement whileBoundStatement)
			{
				var whileSyntax = (WhileStatementSyntax)whileBoundStatement.OriginalNode;
				AddComment(whileSyntax.TokenWhile, whileSyntax.Condition, whileSyntax.TokenDo);
				var startLabel = CodeGen.Generator.DeclareLabel();
				var endLabel = CodeGen.Generator.DeclareLabel();
				CodeGen.Generator.IL_Label(startLabel);
				var value = CodeGen.LoadValueAsVariable(whileBoundStatement.Condition);
				CodeGen.Generator.IL_Jump_IfNot(value, endLabel);
				whileBoundStatement.Body.Accept(GetInLoopVisitor(endLabel, startLabel));
				AddComment(whileSyntax.TokenEndWhile);
				CodeGen.Generator.IL_Jump(startLabel);
				CodeGen.Generator.IL_Label(endLabel);
			}

			public void Visit(ExitBoundStatement exitBoundStatement)
			{
				if (_loopExitLabel is null)
					throw new InvalidOperationException();
				AddComment(exitBoundStatement);
				CodeGen.Generator.IL_Jump(_loopExitLabel);
			}

			public void Visit(ContinueBoundStatement continueBoundStatement)
			{
				if (_loopContinueLabel is null)
					throw new InvalidOperationException();
				AddComment(continueBoundStatement);
				CodeGen.Generator.IL_Jump(_loopContinueLabel);
			}

			public void Visit(ReturnBoundStatement returnBoundStatement)
			{
				AddComment(returnBoundStatement);
				CodeGen.Generator.IL(IR.Return.Instance);
			}
		}
	
		private readonly StatementVisitor _statementVisitor;
		public void CompileStatement(IBoundStatement statement)
		{
			statement.Accept(_statementVisitor);
			Generator.IL(IR.Return.Instance);
		}
	}
}
