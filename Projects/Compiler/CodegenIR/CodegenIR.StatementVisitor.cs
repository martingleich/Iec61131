using System;
using System.Collections.Immutable;
using System.Linq;
using Compiler;
using StandardLibraryExtensions;
using IR = Runtime.IR;
using IRStmt = Runtime.IR.Statements;

namespace Compiler.CodegenIR
{
	public sealed partial class CodegenIR
	{
		private sealed class StatementVisitor : IBoundStatement.IVisitor
		{
			private readonly CodegenIR CodeGen;
			private readonly IRStmt.Label? _loopExitLabel;
			private readonly IRStmt.Label? _loopContinueLabel;
			private ImmutableArray<BreakpointId> _breakpointPredecessors;

			public StatementVisitor(CodegenIR codeGen) : this(codeGen, null, null, ImmutableArray<BreakpointId>.Empty)
			{
			}
			private StatementVisitor(CodegenIR codeGen, IRStmt.Label? loopExitLabel, IRStmt.Label? loopContinueLabel, ImmutableArray<BreakpointId> breakpointPredecessors)
			{
				CodeGen = codeGen ?? throw new ArgumentNullException(nameof(codeGen));
				_loopExitLabel = loopExitLabel;
				_loopContinueLabel = loopContinueLabel;
				_breakpointPredecessors = breakpointPredecessors;
			}

			public void AddTrailingReturn(IBoundStatement boundSt)
			{
				SourceSpan? span =  boundSt.OriginalNode is ISyntax originalSyntax ? originalSyntax.GetFullEnd().WithLength(0) : null;
				using (var _ = NewBreakpointScope(span))
                    CodeGen.Generator.IL(IRStmt.Return.Instance);
			}

			private StatementVisitor GetInLoopVisitor(IRStmt.Label loopExitLabel, IRStmt.Label loopContinueLabel) =>
				new (CodeGen, loopExitLabel, loopContinueLabel, _breakpointPredecessors);

			private void AddComment(IBoundNode boundNode)
			{
				var text = SyntaxToStringConverter.ExactToString(boundNode);
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

			private readonly struct BreakpointScope : IDisposable
			{
				public static readonly BreakpointScope Null = default;
				private readonly StatementVisitor? Self;
				private readonly SourceSpan SourceSpan;
				private readonly int InstructionBegin;

				public BreakpointScope(StatementVisitor? self, SourceSpan sourceSpan, int instructionBegin)
				{
					Self = self;
					SourceSpan = sourceSpan;
					InstructionBegin = instructionBegin;
				}

				public void Dispose()
				{
					if (Self != null)
					{
						var newId = Self.CodeGen.BreakpointFactory.AddBreakpoint(
							SourceSpan,
							IR.Range.Create(InstructionBegin, Self.CodeGen.Generator.InstructionId)); // The End is exclusive
						foreach (var pred in Self._breakpointPredecessors)
							Self.CodeGen.BreakpointFactory.SetPredecessor(newId, pred);
						Self._breakpointPredecessors = ImmutableArray.Create(newId);
					}
				}
			}

			private BreakpointScope NewBreakpointScope(SourceSpan? maybeSourceSpan)
			{
				var instructionBegin = CodeGen.Generator.InstructionId;
				if (maybeSourceSpan is SourceSpan sourceSpan)
					return new BreakpointScope(this, sourceSpan, instructionBegin);
				else
					return BreakpointScope.Null;
			}
			private BreakpointScope NewBreakpointScope(IBoundNode owner) => NewBreakpointScope(owner.TryGetSourcePosition());
			public void Visit(ExpressionBoundStatement expressionBoundStatement)
			{
				using (var _ = NewBreakpointScope(expressionBoundStatement))
				{
                    AddComment(expressionBoundStatement);
					expressionBoundStatement.Expression.Accept(CodeGen._loadValueExpressionVisitor);
				}
			}

			public void Visit(AssignBoundStatement assignToExpressionBoundStatement)
			{
				using (var _ = NewBreakpointScope(assignToExpressionBoundStatement))
				{
                    AddComment(assignToExpressionBoundStatement);
					var writable = CodeGen.LoadWritable(assignToExpressionBoundStatement.LeftSide);
					Assign(writable, assignToExpressionBoundStatement.RightSide);
				}
			}
			public void Visit(InitVariableBoundStatement initVariableBoundStatement)
			{
				if (initVariableBoundStatement.RightSide is not null)
				{
					using (var _ = NewBreakpointScope(initVariableBoundStatement))
					{
                        AddComment(initVariableBoundStatement);
						var writable = initVariableBoundStatement.LeftSide.Accept(CodeGen._variableAddressableVisitor).ToWritable(CodeGen);
						Assign(writable, initVariableBoundStatement.RightSide);
					}
				}
			}

			public void Visit(IfBoundStatement ifBoundStatement)
			{
				var allBranches = ImmutableArray.CreateBuilder<BreakpointId>();
				var endLabel = CodeGen.Generator.DeclareLabel();
				for (int i = 0; i < ifBoundStatement.Branches.Length; ++i)
				{
					var branch = ifBoundStatement.Branches[i];
					if (i == ifBoundStatement.Branches.Length - 1)
					{
						// Last branch.
						if (branch.Condition is IBoundExpression condition)
						{
							using (var _ = NewBreakpointScope(condition))
							{
								AddComment(condition);
								var conditionValue = CodeGen.LoadValueAsVariable(condition);
								CodeGen.Generator.IL_Jump_IfNot(conditionValue, endLabel);
							}
						}
						branch.Body.Accept(this);
						CodeGen.Generator.IL_Label(endLabel);
						allBranches.AddRange(_breakpointPredecessors);
					}
					else
					{
						var nextLabel = CodeGen.Generator.DeclareLabel();
						if (branch.Condition is IBoundExpression condition)
						{
							using (var _ = NewBreakpointScope(condition))
							{
								AddComment(condition);
								var conditionValue = CodeGen.LoadValueAsVariable(condition);
								CodeGen.Generator.IL_Jump_IfNot(conditionValue, nextLabel);
							}
							branch.Body.Accept(this);
							CodeGen.Generator.IL_Jump(endLabel);
							CodeGen.Generator.IL_Label(nextLabel);
							allBranches.AddRange(_breakpointPredecessors);
						}
						else
						{
							branch.Body.Accept(this);
							CodeGen.Generator.IL_Label(endLabel);
							allBranches.AddRange(_breakpointPredecessors);
							break; // Code after this is unreachable.
						}
					}
				}

				_breakpointPredecessors = allBranches.ToImmutable();
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
				var whileSyntax = whileBoundStatement.OriginalNode as WhileStatementSyntax;
				if(whileSyntax != null) AddComment(whileSyntax.TokenWhile, whileSyntax.Condition, whileSyntax.TokenDo);
				var startLabel = CodeGen.Generator.DeclareLabel();
				var endLabel = CodeGen.Generator.DeclareLabel();
				using (var _ = NewBreakpointScope(whileBoundStatement.Condition))
				{
					CodeGen.Generator.IL_Label(startLabel);
					var value = CodeGen.LoadValueAsVariable(whileBoundStatement.Condition);
					CodeGen.Generator.IL_Jump_IfNot(value, endLabel);
				}
				var breakpointCondition = _breakpointPredecessors;
				whileBoundStatement.Body.Accept(GetInLoopVisitor(endLabel, startLabel));
				if(whileSyntax != null) AddComment(whileSyntax.TokenEndWhile);
				using (var _ = NewBreakpointScope(whileSyntax?.TokenEndWhile?.SourceSpan))
				{
					CodeGen.Generator.IL_Jump(startLabel);
					CodeGen.Generator.IL_Label(endLabel);
				}

				foreach(var endBreakpoint in _breakpointPredecessors)
					foreach(var cond in breakpointCondition)
						CodeGen.BreakpointFactory.SetSuccessor(endBreakpoint, cond);
				_breakpointPredecessors = breakpointCondition;
			}

			public void Visit(ExitBoundStatement exitBoundStatement)
			{
				if (_loopExitLabel is null)
					throw new InvalidOperationException();
				AddComment(exitBoundStatement);
				using (var _ = NewBreakpointScope(exitBoundStatement))
				{
					CodeGen.Generator.IL_Jump(_loopExitLabel);
				}
			}

			public void Visit(ContinueBoundStatement continueBoundStatement)
			{
				if (_loopContinueLabel is null)
					throw new InvalidOperationException();
				AddComment(continueBoundStatement);
				using (var _ = NewBreakpointScope(continueBoundStatement))
				{
					CodeGen.Generator.IL_Jump(_loopContinueLabel);
				}
			}

			public void Visit(ReturnBoundStatement returnBoundStatement)
			{
				AddComment(returnBoundStatement);
				using (var _ = NewBreakpointScope(returnBoundStatement))
				{
					CodeGen.Generator.IL(IRStmt.Return.Instance);
				}
			}
		}
	
		private readonly StatementVisitor _statementVisitor;
		public void CompileStatement(IBoundStatement statement)
		{
			statement.Accept(_statementVisitor);
			_statementVisitor.AddTrailingReturn(statement);
		}
	}
}
