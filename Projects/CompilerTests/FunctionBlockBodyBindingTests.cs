namespace Tests
{
	using Compiler;
	using Compiler.Messages;
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
}
