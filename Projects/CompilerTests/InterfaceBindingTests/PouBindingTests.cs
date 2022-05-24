using Compiler;
using Compiler.Messages;
using Xunit;

namespace CompilerTests
{
    using static ErrorHelper;

    public sealed class PouBindingTests
	{
		[Theory]
		[InlineData("FUNCTION", "VAR_INPUT")]
		[InlineData("FUNCTION", "VAR_OUTPUT")]
		[InlineData("FUNCTION", "VAR_IN_OUT")]
		[InlineData("FUNCTION_BLOCK", "VAR_INPUT")]
		[InlineData("FUNCTION_BLOCK", "VAR_OUTPUT")]
		[InlineData("FUNCTION_BLOCK", "VAR_IN_OUT")]
		[InlineData("FUNCTION_BLOCK", "VAR_INST")]
		public void Error_VariableCannotHaveInitalValue(string pouKind, string varKind)
		{
			BindHelper.NewProject
				.AddPou(pouKind, "foo", $"{varKind} value : INT := 0; END_VAR", "")
				.BindBodies(ErrorOfType<VariableCannotHaveInitialValueMessage>());
		}

		[Theory]
		[InlineData("FUNCTION", "VAR_TEMP")]
		[InlineData("FUNCTION_BLOCK", "VAR_TEMP")]
		public void InitialValue(string pouKind, string varKind)
		{
			BindHelper.NewProject
				.AddPou(pouKind, "foo", $"{varKind} value : INT := 6; END_VAR", "value;")
				.BindBodies()
				.Inspect("foo", st =>
				{
					var expression = Assert.IsType<VariableBoundExpression>(AssertEx.AssertNthStatement<ExpressionBoundStatement>(st, 0).Expression);
					var variable = Assert.IsType<LocalVariableSymbol>(expression.Variable);
					var initialExpr = Assert.IsType<LiteralBoundExpression>(variable.InitialValue);
					var value = Assert.IsType<IntLiteralValue>(initialExpr.Value);
					Assert.Equal(6, value.Value);
				});
		}
		[Theory]
		[InlineData("FUNCTION", "VAR_TEMP")]
		[InlineData("FUNCTION_BLOCK", "VAR_TEMP")]
		public void Error_InitialValue_WrongType(string pouKind, string varKind)
		{
			BindHelper.NewProject
				.AddPou(pouKind, "foo", $"{varKind} value : INT := BOOL#FALSE; END_VAR", "")
				.BindBodies(ErrorOfType<TypeIsNotConvertibleMessage>());
		}
		[Theory]
		[InlineData("FUNCTION", "VAR_TEMP")]
		[InlineData("FUNCTION_BLOCK", "VAR_TEMP")]
		public void InitialValue_ReferenceInput(string pouKind, string varKind)
		{
			BindHelper.NewProject
				.AddPou(pouKind, "foo", $"VAR_INPUT inputArg : INT; END_VAR {varKind} value : INT := inputArg; END_VAR", "value;")
				.BindBodies()
				.Inspect("foo", st =>
				{
					var expression = Assert.IsType<VariableBoundExpression>(AssertEx.AssertNthStatement<ExpressionBoundStatement>(st, 0).Expression);
					var variable = Assert.IsType<LocalVariableSymbol>(expression.Variable);
					var initialExpr = Assert.IsType<VariableBoundExpression>(variable.InitialValue);
					AssertEx.EqualCaseInsensitive("inputArg", initialExpr.Variable.Name);
				});
		}

		[Theory]
		[InlineData("FUNCTION", "VAR_TEMP")]
		[InlineData("FUNCTION_BLOCK", "VAR_TEMP")]
		public void InitialValue_CannotReadTemp(string pouKind, string varKind)
		{
			BindHelper.NewProject
				.AddPou(pouKind, "foo", $"{varKind} otherTemp : INT; END_VAR {varKind} value : INT := otherTemp; END_VAR", "")
				.BindBodies(ErrorOfType<VariableNotFoundMessage>(msg => AssertEx.EqualCaseInsensitive("otherTemp", msg.Identifier)));
		}
	}
}
