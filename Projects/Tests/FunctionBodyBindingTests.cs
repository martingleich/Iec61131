namespace Tests
{
	using Compiler;
	using Compiler.Messages;
	using System;
	using Xunit;
	using static ErrorTestHelper;
	public sealed class FunctionBodyBindingTests
	{
		[Fact]
		public void BindEmptyBody()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo", "")
				.BindBodies()
				.Inspect("foo", st =>
				{
					var seq = Assert.IsType<SequenceBoundStatement>(st);
					Assert.Empty(seq.Statements);
				});
		}
		[Fact]
		public void BindEmptyStatement()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo", ";")
				.BindBodies()
				.Inspect("foo", st =>
				{
					var seq2 = AssertEx.AssertNthStatement<SequenceBoundStatement>(st, 0);
					Assert.Empty(seq2.Statements);
				});
		}
		[Theory]
		[InlineData("5", typeof(LiteralBoundExpression))]
		[InlineData("x", typeof(VariableBoundExpression))]
		[InlineData("x + 1", typeof(BinaryOperatorBoundExpression))]
		[InlineData("SIZEOF(INT)", typeof(SizeOfTypeBoundExpression))]
		public void BindExpressionStatement(string expr, Type exprType)
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo VAR x : INT; END_VAR", expr + ";")
				.BindBodies()
				.Inspect("foo", st =>
				{
					var exprSt = AssertEx.AssertNthStatement<ExpressionBoundStatement>(st, 0);
					Assert.IsType(exprType, exprSt.Expression);
				});
		}

		[Fact]
		public void Error_OnDuplicateVarName()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo VAR x : INT; x : REAL; END_VAR", "")
				.BindBodies(ErrorOfType<SymbolAlreadyExistsMessage>(msg => Assert.Equal("x".ToCaseInsensitive(), msg.Name)));
		}
		[Fact]
		public void Error_OnDuplicateVarNameAndArgument()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo VAR_INPUT x : INT; END_VAR VAR x : REAL; END_VAR", "")
				.BindBodies(ErrorOfType<SymbolAlreadyExistsMessage>(msg => Assert.Equal("x".ToCaseInsensitive(), msg.Name)));
		}
		[Fact]
		public void Namelookup_Argument()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo VAR_INPUT x : INT; END_VAR", "x;")
				.BindBodies()
				.Inspect("foo", st =>
				{
					var exprSt = AssertEx.AssertNthStatement<ExpressionBoundStatement>(st, 0);
					var expr = Assert.IsType<VariableBoundExpression>(exprSt.Expression);
					Assert.Equal("x".ToCaseInsensitive(), expr.Variable.Name);
				});
		}
		[Fact]
		public void Namelookup_Local()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo VAR y : INT; END_VAR", "y;")
				.BindBodies()
				.Inspect("foo", st =>
				{
					var exprSt = AssertEx.AssertNthStatement<ExpressionBoundStatement>(st, 0);
					var expr = Assert.IsType<VariableBoundExpression>(exprSt.Expression);
					Assert.Equal("y".ToCaseInsensitive(), expr.Variable.Name);
				});
		}
		[Fact]
		public void Error_Namelookup_NoExist()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo", "doesNotExist;")
				.BindBodies(ErrorOfType<VariableNotFoundMessage>(msg => Assert.Equal("doesNotExist".ToCaseInsensitive(), msg.Identifier)));
		}
		[Fact]
		public void AssignStatement()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo VAR x : INT; END_VAR", "x := 5;")
				.BindBodies()
				.Inspect("foo", st =>
				{
					var boundSt = AssertEx.AssertNthStatement<AssignBoundStatement>(st, 0);
					var left = Assert.IsType<VariableBoundExpression>(boundSt.LeftSide);
					var right = Assert.IsType<LiteralBoundExpression>(boundSt.RightSide);
				});
		}
		[Fact]
		public void Error_AssignStatement_InvalidLValueSyntax()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo VAR x : INT; END_VAR", "(x + 5) := 5;")
				.BindBodies(ErrorOfType<CannotAssignToSyntaxMessage>());
		}

		[Fact]
		public void AssignStatement_ImplicitCast()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo VAR x : REAL; END_VAR", "x := DINT#25;")
				.BindBodies()
				.Inspect("foo", st =>
				{
					var boundSt = AssertEx.AssertNthStatement<AssignBoundStatement>(st, 0);
					var left = Assert.IsType<VariableBoundExpression>(boundSt.LeftSide);
					var right = Assert.IsType<ImplicitArithmeticCastBoundExpression>(boundSt.RightSide);
				});
		}

		[Fact]
		public void Error_AssignStatement_IncompatibleTypes()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo VAR x : BOOL; END_VAR", "x := 5;")
				.BindBodies(ErrorOfType<IntegerIsToLargeForTypeMessage>());
		}

		[Fact]
		public void IfStatement_IfOnly()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo VAR xc : BOOL; xb : INT; END_VAR", "IF xc THEN xb; END_IF")
				.BindBodies()
				.Inspect("foo", st =>
				{
					var ifSt = AssertEx.AssertNthStatement<IfBoundStatement>(st, 0);
					var branch = Assert.Single(ifSt.Branches);
					AssertEx.AssertVariableExpression(branch.Condition, "xc");
					AssertEx.AssertStatementBlockMarker(branch.Body, "xb");
				});
		}
		[Fact]
		public void IfStatement_IfWithElse()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo VAR xc : BOOL; xb : INT; yb : INT; END_VAR", "IF xc THEN xb; ELSE yb; END_IF")
				.BindBodies()
				.Inspect("foo", st =>
				{
					var ifSt = AssertEx.AssertNthStatement<IfBoundStatement>(st, 0);
					Assert.Collection(ifSt.Branches,
						b =>
						{
							AssertEx.AssertVariableExpression(b.Condition, "xc");
							AssertEx.AssertStatementBlockMarker(b.Body, "xb");
						},
						b =>
						{
							Assert.Null(b.Condition);
							AssertEx.AssertStatementBlockMarker(b.Body, "yb");
						});
				});
		}
		[Fact]
		public void IfStatement_IfWithMultipleElsIf()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo VAR xc : BOOL; xb : INT; yc : BOOL; yb : INT; zc : BOOL; zb : INT; END_VAR", "IF xc THEN xb; ELSIF yc THEN yb; ELSIF zc THEN zb; END_IF")
				.BindBodies()
				.Inspect("foo", st =>
				{
					var ifSt = AssertEx.AssertNthStatement<IfBoundStatement>(st, 0);
					Assert.Collection(ifSt.Branches,
						b =>
						{
							AssertEx.AssertVariableExpression(b.Condition, "xc");
							AssertEx.AssertStatementBlockMarker(b.Body, "xb");
						},
						b =>
						{
							AssertEx.AssertVariableExpression(b.Condition, "yc");
							AssertEx.AssertStatementBlockMarker(b.Body, "yb");
						},
						b =>
						{
							AssertEx.AssertVariableExpression(b.Condition, "zc");
							AssertEx.AssertStatementBlockMarker(b.Body, "zb");
						});
				});
		}
		[Fact]
		public void IfStatement_IfWithElsIfElse()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo VAR xc : BOOL; xb : INT; yb : INT; yc : BOOL; zb : INT; END_VAR", "IF xc THEN xb; ELSIF yc THEN yb; ELSE zb; END_IF")
				.BindBodies()
				.Inspect("foo", st =>
				{
					var ifSt = AssertEx.AssertNthStatement<IfBoundStatement>(st, 0);
					Assert.Collection(ifSt.Branches,
						b =>
						{
							AssertEx.AssertVariableExpression(b.Condition, "xc");
							AssertEx.AssertStatementBlockMarker(b.Body, "xb");
						},
						b =>
						{
							AssertEx.AssertVariableExpression(b.Condition, "yc");
							AssertEx.AssertStatementBlockMarker(b.Body, "yb");
						},
						b =>
						{
							Assert.Null(b.Condition);
							AssertEx.AssertStatementBlockMarker(b.Body, "zb");
						});
				});
		}


		[Fact]
		public void WhileStatement()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo VAR xc : BOOL; xb : INT; END_VAR", "WHILE xc DO xb; END_WHILE")
				.BindBodies()
				.Inspect("foo", st =>
				{
					var whileSt = AssertEx.AssertNthStatement<WhileBoundStatement>(st, 0);
					AssertEx.AssertVariableExpression(whileSt.Condition, "xc");
					AssertEx.AssertStatementBlockMarker(whileSt.Body, "xb");
				});
		}
		[Fact]
		public void WhileStatement_WithExit()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo VAR xc : BOOL; END_VAR", "WHILE xc DO EXIT; END_WHILE")
				.BindBodies()
				.Inspect("foo", st =>
				{
					var whileSt = AssertEx.AssertNthStatement<WhileBoundStatement>(st, 0);
					AssertEx.AssertNthStatement<ExitBoundStatement>(whileSt.Body, 0);
				});
		}
		[Fact]
		public void WhileStatement_WithContinue()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo VAR xc : BOOL; END_VAR", "WHILE xc DO CONTINUE; END_WHILE")
				.BindBodies()
				.Inspect("foo", st =>
				{
					var whileSt = AssertEx.AssertNthStatement<WhileBoundStatement>(st, 0);
					AssertEx.AssertNthStatement<ContinueBoundStatement>(whileSt.Body, 0);
				});
		}
		[Theory]
		[InlineData("EXIT;")]
		[InlineData("CONTINUE;")]
		public void Error_SyntaxOnlyInLoop(string syntax)
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo", syntax)
				.BindBodies(ErrorOfType<SyntaxOnlyAllowedInLoopMessage>());
		}
		[Fact]
		public void Return()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo", "RETURN;")
				.BindBodies()
				.Inspect("foo", st =>
				{
					AssertEx.AssertNthStatement<ReturnBoundStatement>(st, 0);
				});
		}
	}
}
