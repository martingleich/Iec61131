namespace Tests
{
	using Compiler;
	using Compiler.Messages;
	using Xunit;
	using static ErrorHelper;

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
}
