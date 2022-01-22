namespace Tests
{
	using Compiler;
	using Compiler.Messages;
	using System;
	using Xunit;
	using static ErrorHelper;
	public sealed class FunctionBlockBodyBindingTests
	{
		[Theory]
		[InlineData("VAR_INPUT")]
		[InlineData("VAR_INST")]
		public void Error_Temp_XXX_Collision(string kind)
		{
			BindHelper.NewProject
				.AddFunctionBlock("foo", $"{kind} x : INT; END_VAR VAR_TEMP x : REAL; END_VAR", "")
				.BindBodies(ErrorOfType<SymbolAlreadyExistsMessage>(msg => Assert.Equal("x".ToCaseInsensitive(), msg.Name)));
		}

		[Theory]
		[InlineData("VAR_INPUT")]
		[InlineData("VAR_INST")]
		[InlineData("VAR_TEMP")]
		public void Namelookup(string kind)
		{
			BindHelper.NewProject
				.AddFunctionBlock("foo", $"{kind} x : INT; END_VAR", "x;")
				.BindBodies()
				.Inspect("foo", st =>
				{
					var exprSt = AssertEx.AssertNthStatement<ExpressionBoundStatement>(st, 0);
					var expr = Assert.IsType<VariableBoundExpression>(exprSt.Expression);
					Assert.Equal("x".ToCaseInsensitive(), expr.Variable.Name);
				});
		}
	}

	public sealed class FunctionBodyBindingTests
	{
		[Fact]
		public void BindEmptyBody()
		{
			BindHelper.NewProject
				.AddFunction("foo", "", "")
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
				.AddFunction("foo", "", ";")
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
				.AddFunction("foo", "VAR_TEMP x : INT; END_VAR", expr + ";")
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
				.AddFunction("foo", "VAR_TEMP x : INT; x : REAL; END_VAR", "")
				.BindBodies(ErrorOfType<SymbolAlreadyExistsMessage>(msg => Assert.Equal("x".ToCaseInsensitive(), msg.Name)));
		}
		[Fact]
		public void Error_OnDuplicateVarNameAndArgument()
		{
			BindHelper.NewProject
				.AddFunction("foo", "VAR_INPUT x : INT; END_VAR VAR_TEMP x : REAL; END_VAR", "")
				.BindBodies(ErrorOfType<SymbolAlreadyExistsMessage>(msg => Assert.Equal("x".ToCaseInsensitive(), msg.Name)));
		}
		[Fact]
		public void Namelookup_Argument()
		{
			BindHelper.NewProject
				.AddFunction("foo", "VAR_INPUT x : INT; END_VAR", "x;")
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
				.AddFunction("foo", "VAR_TEMP y : INT; END_VAR", "y;")
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
				.AddFunction("foo", "", "doesNotExist;")
				.BindBodies(ErrorOfType<VariableNotFoundMessage>(msg => Assert.Equal("doesNotExist".ToCaseInsensitive(), msg.Identifier)));
		}
		[Fact]
		public void AssignStatement()
		{
			BindHelper.NewProject
				.AddFunction("foo", "VAR_TEMP x : INT; END_VAR", "x := 5;")
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
				.AddFunction("foo", "VAR_TEMP x : INT; END_VAR", "(x + 5) := 5;")
				.BindBodies(ErrorOfType<CannotAssignToSyntaxMessage>());
		}

		[Fact]
		public void AssignStatement_ImplicitCast()
		{
			BindHelper.NewProject
				.AddFunction("foo", "VAR_TEMP x : REAL; END_VAR", "x := DINT#25;")
				.BindBodies()
				.Inspect("foo", st =>
				{
					var boundSt = AssertEx.AssertNthStatement<AssignBoundStatement>(st, 0);
					var left = Assert.IsType<VariableBoundExpression>(boundSt.LeftSide);
					var right = Assert.IsType<ImplicitCastBoundExpression>(boundSt.RightSide);
				});
		}

		[Fact]
		public void Error_AssignStatement_IncompatibleTypes()
		{
			BindHelper.NewProject
				.AddFunction("foo", "VAR_TEMP x : BOOL; END_VAR", "x := 5;")
				.BindBodies(ErrorOfType<ConstantDoesNotFitIntoTypeMessage>());
		}

		[Fact]
		public void IfStatement_IfOnly()
		{
			BindHelper.NewProject
				.AddFunction("foo", "VAR_TEMP xc : BOOL; xb : INT; END_VAR", "IF xc THEN xb; END_IF")
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
				.AddFunction("foo", "VAR_TEMP xc : BOOL; xb : INT; yb : INT; END_VAR", "IF xc THEN xb; ELSE yb; END_IF")
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
				.AddFunction("foo", "VAR_TEMP xc : BOOL; xb : INT; yc : BOOL; yb : INT; zc : BOOL; zb : INT; END_VAR", "IF xc THEN xb; ELSIF yc THEN yb; ELSIF zc THEN zb; END_IF")
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
				.AddFunction("foo", "VAR_TEMP xc : BOOL; xb : INT; yb : INT; yc : BOOL; zb : INT; END_VAR", "IF xc THEN xb; ELSIF yc THEN yb; ELSE zb; END_IF")
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
				.AddFunction("foo", "VAR_TEMP xc : BOOL; xb : INT; END_VAR", "WHILE xc DO xb; END_WHILE")
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
				.AddFunction("foo", "VAR_TEMP xc : BOOL; END_VAR", "WHILE xc DO EXIT; END_WHILE")
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
				.AddFunction("foo", "VAR_TEMP xc : BOOL; END_VAR", "WHILE xc DO CONTINUE; END_WHILE")
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
				.AddFunction("foo", "", syntax)
				.BindBodies(ErrorOfType<SyntaxOnlyAllowedInLoopMessage>());
		}
		[Fact]
		public void Return()
		{
			BindHelper.NewProject
				.AddFunction("foo", "", "RETURN;")
				.BindBodies()
				.Inspect("foo", st =>
				{
					AssertEx.AssertNthStatement<ReturnBoundStatement>(st, 0);
				});
		}
	}

	public sealed class StatementBindingTests_LocalVarDecl
	{
		[Fact]
		public static void LocalVar_InitOnly()
		{
			var bodies = BindHelper.NewProject
				.AddFunction("foo", "", "VAR x := FALSE; x;")
				.BindBodies();
			bodies.Inspect("foo", st =>
			{
				var initVarSt = AssertEx.AssertNthStatement<InitVariableBoundStatement>(st, 0);
				AssertEx.EqualCaseInsensitive("x", initVarSt.LeftSide.Name);
				AssertEx.EqualType(bodies.SystemScope.Bool, initVarSt.LeftSide.Type);
				AssertEx.HasConstantValue(initVarSt.RightSide, bodies.SystemScope, AssertEx.LiteralBool(false));
				var varExpr = AssertEx.AssertNthStatement<ExpressionBoundStatement>(st, 1);
				AssertEx.AssertVariableExpression(varExpr.Expression, "x");
			});
		}
		[Fact]
		public static void LocalVar_InitAndType()
		{
			var bodies = BindHelper.NewProject
				.AddFunction("foo", "", "VAR x : BOOL := FALSE; x;")
				.BindBodies();
			bodies.Inspect("foo", st =>
			{
				var initVarSt = AssertEx.AssertNthStatement<InitVariableBoundStatement>(st, 0);
				AssertEx.EqualCaseInsensitive("x", initVarSt.LeftSide.Name);
				AssertEx.EqualType(bodies.SystemScope.Bool, initVarSt.LeftSide.Type);
				AssertEx.HasConstantValue(initVarSt.RightSide, bodies.SystemScope, AssertEx.LiteralBool(false));
				var varExpr = AssertEx.AssertNthStatement<ExpressionBoundStatement>(st, 1);
				AssertEx.AssertVariableExpression(varExpr.Expression, "x");
			});
		}
		[Fact]
		public static void LocalVar_TypeOnly()
		{
			var bodies = BindHelper.NewProject
				.AddFunction("foo", "", "VAR x : BOOL; x := 0; x;")
				.BindBodies();
			bodies.Inspect("foo", st =>
			{
				var initVarSt = AssertEx.AssertNthStatement<InitVariableBoundStatement>(st, 0);
				AssertEx.EqualCaseInsensitive("x", initVarSt.LeftSide.Name);
				AssertEx.EqualType(bodies.SystemScope.Bool, initVarSt.LeftSide.Type);
				Assert.Null(initVarSt.RightSide);
				var varExpr = AssertEx.AssertNthStatement<ExpressionBoundStatement>(st, 2);
				AssertEx.AssertVariableExpression(varExpr.Expression, "x");
			});
		}
		
		[Fact]
		public static void Error_LocalVar_TypeInitMismatch()
		{
			BindHelper.NewProject
				.AddFunction("foo", "", "VAR x : INT := TRUE;")
				.BindBodies(ErrorOfType<TypeIsNotConvertibleMessage>());
		}
		[Fact]
		public static void Error_LocalVar_NoTypeNoInit()
		{
			BindHelper.NewProject
				.AddFunction("foo", "", "VAR x;")
				.BindBodies(ErrorOfType<CannotInferTypeOfVariableMessage>());
		}
		[Fact]
		public static void Error_MultipleVariables()
		{
			BindHelper.NewProject
				.AddFunction("foo", "", "VAR x : INT; VAR x : BOOL;")
				.BindBodies(ErrorOfType<SymbolAlreadyExistsMessage>());
		}
		[Fact]
		public static void Error_ShadowingVariable_InIfBlock()
		{
			BindHelper.NewProject
				.AddFunction("foo", "", "VAR x : INT; IF TRUE THEN VAR x : BOOL; END_IF")
				.BindBodies(ErrorOfType<ShadowedLocalVariableMessage>());
		}
		[Fact]
		public static void Error_ShadowingVariable_VarTempBlock()
		{
			BindHelper.NewProject
				.AddFunction("foo", "VAR_TEMP x : INT; END_VAR", "VAR x : INT;")
				.BindBodies(ErrorOfType<ShadowedLocalVariableMessage>());
		}
		[Fact]
		public static void Error_UseVariableBeforeDeclared()
		{
			BindHelper.NewProject
				.AddFunction("foo", "", "x := 7; VAR x : INT;")
				.BindBodies(ErrorOfType<CannotUseVariableBeforeItIsDeclaredMessage>());
		}
		[Fact]
		public static void SameVariableInDiffrentBlocks()
		{
			BindHelper.NewProject
				.AddFunction("foo", "VAR_INPUT x : BOOL; END_VAR", "IF x THEN VAR y : INT := 0; y; ELSE VAR y : REAL := 0; y; END_IF")
				.BindBodies();
		}
		[Fact]
		public static void Error_FlowAnalyis_UseVariableBeforeWritten()
		{
			BindHelper.NewProject
				.AddFunction("foo", "", "VAR x : INT; x; x := 0;")
				.BindBodies()
				.InspectFlowMessages("foo", ErrorOfType<UseOfUnassignedVariableMessage>());
		}
		[Fact]
		public static void Error_FlowAnalyis_NoErrorForUseBeforeDeclare()
		{
			BindHelper.NewProject
				.AddFunction("foo", "", "x; VAR x : INT;")
				.BindBodies(ErrorOfType<CannotUseVariableBeforeItIsDeclaredMessage>())
				.InspectFlowMessages("foo");
		}
	}

	public static class StatementBindingTests_ForLoop
	{
		[Theory]
		[InlineData("SINT")]
		[InlineData("USINT")]
		[InlineData("INT")]
		[InlineData("UINT")]
		[InlineData("DINT")]
		[InlineData("UDINT")]
		[InlineData("LINT")]
		[InlineData("ULINT")]
		[InlineData("REAL")]
		[InlineData("LREAL")]
		public static void WithType(string type)
		{
			BindHelper.NewProject
				.AddFunction("foo", $"VAR_TEMP i : {type}; END_VAR", "FOR i := 0 TO 10 DO ; END_FOR")
				.BindBodies()
				.Inspect("foo", st =>
				{
					var forSt = AssertEx.AssertNthStatement<ForLoopBoundStatement>(st, 0);
					var indexVar = Assert.IsType<VariableBoundExpression>(forSt.Index);
					Assert.Equal("i".ToCaseInsensitive(), indexVar.Variable.Name);
					var initialVar = Assert.IsType<LiteralBoundExpression>(forSt.Initial);
					AssertEx.EqualType(indexVar.Type, initialVar.Value.Type);
					var step = Assert.IsType<LiteralBoundExpression>(forSt.Step);
					AssertEx.EqualType(indexVar.Type, step.Value.Type);
					AssertEx.AssertNthStatement<SequenceBoundStatement>(forSt.Body, 0);
				});
		}
		[Fact]
		public static void StepSize1()
		{
			BindHelper.NewProject
				.AddFunction("foo", $"VAR_TEMP i : INT; END_VAR", "FOR i := 0 TO 10 DO ; END_FOR")
				.BindBodies()
				.Inspect("foo", st =>
				{
					var forSt = AssertEx.AssertNthStatement<ForLoopBoundStatement>(st, 0);
					var step = Assert.IsType<LiteralBoundExpression>(forSt.Step);
					Assert.Equal(1, Assert.IsType<IntLiteralValue>(step.Value).Value);
					AssertEx.AssertNthStatement<SequenceBoundStatement>(forSt.Body, 0);
				});
		}
		
		[Fact]
		public static void ExplicitStep()
		{
			BindHelper.NewProject
				.AddFunction("foo", $"VAR_TEMP i : INT; END_VAR", "FOR i := 10 TO 0 BY -1 DO ; END_FOR")
				.BindBodies()
				.Inspect("foo", st =>
				{
					var forSt = AssertEx.AssertNthStatement<ForLoopBoundStatement>(st, 0);
					var step = Assert.IsType<LiteralBoundExpression>(forSt.Step);
					Assert.Equal(-1, Assert.IsType<IntLiteralValue>(step.Value).Value);
					AssertEx.AssertNthStatement<SequenceBoundStatement>(forSt.Body, 0);
				});
		}
		
		[Fact]
		public static void CastedStep()
		{
			BindHelper.NewProject
				.AddFunction("foo", $"VAR_TEMP i : DINT; END_VAR", "FOR i := 10 TO 0 BY SINT#2 DO ; END_FOR")
				.BindBodies()
				.Inspect("foo", st =>
				{
					var forSt = AssertEx.AssertNthStatement<ForLoopBoundStatement>(st, 0);
					Assert.IsType<ImplicitCastBoundExpression>(forSt.Step);
				});
		}
		
		[Fact]
		public static void Error_NonAddableType()
		{
			BindHelper.NewProject
				.AddFunction("foo", $"VAR_TEMP i : BOOL; END_VAR", "FOR i := FALSE TO TRUE BY FALSE DO ; END_FOR")
				.BindBodies(ErrorOfType<CannotUseTypeAsLoopIndexMessage>());
		}
		
		[Fact]
		public static void Error_BadStep()
		{
			BindHelper.NewProject
				.AddFunction("foo", $"VAR_TEMP i : INT; END_VAR", "FOR i := 0 TO 10 BY LREAL#5 DO ; END_FOR")
				.BindBodies(ErrorOfType<TypeIsNotConvertibleMessage>());
		}

		[Fact]
		public static void Error_IndexNotAssignable()
		{
			BindHelper.NewProject
				.AddFunction("foo", "", "FOR 19 := 0 TO 10 BY 5 DO ; END_FOR")
				.BindBodies(ErrorOfType<CannotAssignToSyntaxMessage>());
		}
		
		[Fact]
		public static void ComplexIndex_Deref()
		{
			BindHelper.NewProject
				.AddFunction("foo", $"VAR_TEMP ptr : POINTER TO INT; END_VAR", "FOR ptr^ := 0 TO 10 BY 5 DO ; END_FOR")
				.BindBodies();
		}
		
		[Fact]
		public static void ComplexIndex_PointerIndex()
		{
			BindHelper.NewProject
				.AddFunction("foo", $"VAR_TEMP ptr : POINTER TO INT; END_VAR", "FOR ptr[2] := 0 TO 10 BY 5 DO ; END_FOR")
				.BindBodies();
		}
		[Fact]
		public static void ComplexIndex_ArrayElem()
		{
			BindHelper.NewProject
				.AddFunction("foo", $"VAR_TEMP arr : ARRAY[0..10] OF INT; END_VAR", "FOR arr[1] := 0 TO 10 BY 5 DO ; END_FOR")
				.BindBodies();
		}
		[Fact]
		public static void ComplexIndex_Field()
		{
			BindHelper.NewProject
				.AddDut("myDut", "STRUCT field : INT; END_STRUCT")
				.AddFunction("foo", $"VAR_TEMP dut : myDut; END_VAR", "FOR dut.field := 0 TO 10 BY 5 DO ; END_FOR")
				.BindBodies();
		}
		
		[Fact]
		public static void CastedInitial()
		{
			BindHelper.NewProject
				.AddFunction("foo", $"VAR_TEMP i : DINT; END_VAR", "FOR i := INT#5 TO 10 DO ; END_FOR")
				.BindBodies()
				.Inspect("foo", st =>
				{
					var forSt = AssertEx.AssertNthStatement<ForLoopBoundStatement>(st, 0);
					Assert.IsType<ImplicitCastBoundExpression>(forSt.Initial);
				});
		}

		[Fact]
		public static void WithContinue()
		{
			BindHelper.NewProject
				.AddFunction("foo", "VAR_TEMP i : INT; END_VAR", "FOR i := 0 TO 10 DO CONTINUE; END_FOR")
				.BindBodies()
				.Inspect("foo", st =>
				{
					var whileSt = AssertEx.AssertNthStatement<ForLoopBoundStatement>(st, 0);
					AssertEx.AssertNthStatement<ContinueBoundStatement>(whileSt.Body, 0);
				});
		}

		[Fact]
		public static void WithExit()
		{
			BindHelper.NewProject
				.AddFunction("foo", "VAR_TEMP i : INT; END_VAR", "FOR i := 0 TO 10 DO EXIT; END_FOR")
				.BindBodies()
				.Inspect("foo", st =>
				{
					var whileSt = AssertEx.AssertNthStatement<ForLoopBoundStatement>(st, 0);
					AssertEx.AssertNthStatement<ExitBoundStatement>(whileSt.Body, 0);
				});
		}

		[Fact]
		public static void LocalVar_NoType()
		{
			var bodies = BindHelper.NewProject
				.AddFunction("foo", "", "FOR VAR i := 0 TO 10 DO END_FOR")
				.BindBodies();
				bodies.Inspect("foo", st =>
				{
					var forSt = AssertEx.AssertNthStatement<ForLoopBoundStatement>(st, 0);
					var indexVarExpr = Assert.IsType<VariableBoundExpression>(forSt.Index);
					AssertEx.EqualCaseInsensitive("i", indexVarExpr.Variable.Name);
					AssertEx.EqualType(bodies.SystemScope.Int, indexVarExpr.Variable.Type);
					var initial = Assert.IsType<LiteralBoundExpression>(forSt.Initial);
					AssertEx.HasConstantValue(initial, bodies.SystemScope, AssertEx.LiteralInt(0));
				});
		}
		
		[Fact]
		public static void LocalVar_Type()
		{
			var bodies = BindHelper.NewProject
				.AddFunction("foo", "", "FOR VAR i : DINT := 0 TO 10 DO END_FOR")
				.BindBodies();
				bodies.Inspect("foo", st =>
				{
					var forSt = AssertEx.AssertNthStatement<ForLoopBoundStatement>(st, 0);
					var indexVarExpr = Assert.IsType<VariableBoundExpression>(forSt.Index);
					AssertEx.EqualCaseInsensitive("i", indexVarExpr.Variable.Name);
					AssertEx.EqualType(bodies.SystemScope.DInt, indexVarExpr.Variable.Type);
					var initial = Assert.IsType<LiteralBoundExpression>(forSt.Initial);
					AssertEx.HasConstantValue(initial, bodies.SystemScope, AssertEx.LiteralDInt(0));
				});
		}

		[Fact]
		public static void LocalVar_IncompatibleType_Error()
		{
			BindHelper.NewProject
				.AddFunction("foo", "", "FOR VAR i : INT := DINT#1000000 TO 10 DO END_FOR")
				.BindBodies(ErrorOfType<TypeIsNotConvertibleMessage>());
		}

		[Fact]
		public static void LocalVarShadowing_Error()
		{
			BindHelper.NewProject
				.AddFunction("foo", "VAR_TEMP i : INT; END_VAR", "FOR VAR i := 0 TO 10 DO END_FOR")
				.BindBodies(ErrorOfType<ShadowedLocalVariableMessage>(msg => AssertEx.EqualCaseInsensitive("i", msg.InnerVariable.Name)));
		}

		[Fact]
		public static void LocalVar_UseInsideBlock()
		{
			var bodies = BindHelper.NewProject
				.AddFunction("foo", "", "FOR VAR i := 0 TO 10 DO i; END_FOR")
				.BindBodies();
			bodies.Inspect("foo", st =>
			{
				var forSt = AssertEx.AssertNthStatement<ForLoopBoundStatement>(st, 0);
				var exprSt = AssertEx.AssertNthStatement<ExpressionBoundStatement>(forSt.Body, 0);
				AssertEx.AssertVariableExpression(exprSt.Expression, "i");
			});
		}
		[Fact]
		public static void LocalVar_AfterBlock_Error()
		{
			BindHelper.NewProject
				.AddFunction("foo", "", "FOR VAR i := 0 TO 10 DO END_FOR i;")
				.BindBodies(ErrorOfType<VariableNotFoundMessage>(msg => AssertEx.EqualCaseInsensitive("i", msg.Identifier)));
		}
		[Fact]
		public static void LocalVar_UpperBound_Error()
		{
			BindHelper.NewProject
				.AddFunction("foo", "", "FOR VAR i := 0 TO i DO END_FOR")
				.BindBodies(ErrorOfType<VariableNotFoundMessage>(msg => AssertEx.EqualCaseInsensitive("i", msg.Identifier)));
		}
		[Fact]
		public static void LocalVar_Step_Error()
		{
			BindHelper.NewProject
				.AddFunction("foo", "", "FOR VAR i := 0 TO 10 BY i DO END_FOR")
				.BindBodies(ErrorOfType<VariableNotFoundMessage>(msg => AssertEx.EqualCaseInsensitive("i", msg.Identifier)));
		}
	}
}
