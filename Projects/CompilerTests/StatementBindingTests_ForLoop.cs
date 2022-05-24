namespace CompilerTests
{
	using Compiler;
	using Compiler.Messages;
	using Xunit;
	using static ErrorHelper;

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
	
		[Theory]
		[InlineData("BOOL")]
		[InlineData("REAL")]
		[InlineData("LREAL")]
		[InlineData("STRING[20]")]
		[InlineData("TIME")]
		[InlineData("DATE")]
		[InlineData("DT")]
		public static void Error_NonAddableType(string type)
		{
			BindHelper.NewProject
				.AddFunction("foo", $"VAR_INPUT x : {type}; END_VAR VAR_TEMP i : {type}; END_VAR", "FOR i := x TO x BY x DO ; END_FOR")
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
		[Fact]
		public static void Error_CannotAssignToLocalVar()
		{
			BindHelper.NewProject
				.AddFunction("foo", "", "FOR VAR i := 0 TO 10 DO i := 0; END_FOR")
				.BindBodies(ErrorOfType<CannotAssignToVariableMessage>(msg => AssertEx.EqualCaseInsensitive("i", msg.Variable.Name)));
		}
	}
}
