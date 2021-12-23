using Compiler;
using Compiler.Messages;
using Xunit;

namespace Tests
{
	using static ErrorHelper;

	public sealed class FunctionBindingTests
	{
		[Fact]
		public void EmptyFunction()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("FUNCTION MyFunction", "")
				.BindInterfaces();
			var myFunction = boundInterface.FunctionSymbols["MyFunction"].Type;
			Assert.Empty(myFunction.Parameters);
		}

		[Fact]
		public void Function_WithInput()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("FUNCTION MyFunction VAR_INPUT myInput : INT; END_VAR", "")
				.BindInterfaces();
			var myFunction = boundInterface.FunctionSymbols["MyFunction"].Type;
			Assert.Collection(myFunction.Parameters,
				p => { Assert.Equal(ParameterKind.Input, p.Kind); Assert.Equal("myInput".ToCaseInsensitive(), p.Name); Assert.Equal(boundInterface.SystemScope.Int, p.Type); });
		}

		[Fact]
		public void Function_WithOutput()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("FUNCTION MyFunction VAR_OUTPUT myOutput : BOOL; END_VAR", "")
				.BindInterfaces();
			var myFunction = boundInterface.FunctionSymbols["MyFunction"].Type;
			Assert.Collection(myFunction.Parameters,
				p => { Assert.Equal(ParameterKind.Output, p.Kind); Assert.Equal("myOutput".ToCaseInsensitive(), p.Name); Assert.Equal(boundInterface.SystemScope.Bool, p.Type); });
		}
		[Fact]
		public void Function_WithInOut()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("FUNCTION MyFunction VAR_IN_OUT myInOut : REAL; END_VAR", "")
				.BindInterfaces();
			var myFunction = boundInterface.FunctionSymbols["MyFunction"].Type;
			Assert.Collection(myFunction.Parameters,
				p => { Assert.Equal(ParameterKind.InOut, p.Kind); Assert.Equal("myInOut".ToCaseInsensitive(), p.Name); Assert.Equal(boundInterface.SystemScope.Real, p.Type); });
		}
		[Fact]
		public void Function_TempIsIgnored()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("FUNCTION MyFunction VAR_TEMP myTemp : REAL; END_VAR", "")
				.BindInterfaces();
			var myFunction = boundInterface.FunctionSymbols["MyFunction"].Type;
			Assert.Empty(myFunction.Parameters);
		}
		[Fact]
		public void Function_VarIsIgnored()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("FUNCTION MyFunction VAR myTemp : REAL; END_VAR", "")
				.BindInterfaces();
			var myFunction = boundInterface.FunctionSymbols["MyFunction"].Type;
			Assert.Empty(myFunction.Parameters);
		}
		[Fact]
		public void Function_InputsInSameBlock()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("FUNCTION MyFunction VAR_INPUT input1 : REAL; input2 : INT; END_VAR", "")
				.BindInterfaces();
			var myFunction = boundInterface.FunctionSymbols["MyFunction"].Type;
			Assert.Collection(myFunction.Parameters,
				p => Assert.Equal("input1".ToCaseInsensitive(), p.Name),
				p => Assert.Equal("input2".ToCaseInsensitive(), p.Name));
		}
		[Fact]
		public void Function_InputsInDiffrentBlock()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("FUNCTION MyFunction VAR_INPUT input1 : REAL; END_VAR VAR_INPUT input2 : INT; END_VAR", "")
				.BindInterfaces();
			var myFunction = boundInterface.FunctionSymbols["MyFunction"].Type;
			Assert.Collection(myFunction.Parameters,
				p => Assert.Equal("input1".ToCaseInsensitive(), p.Name),
				p => Assert.Equal("input2".ToCaseInsensitive(), p.Name));
		}
		[Fact]
		public void Function_ReturnAsOutput()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("FUNCTION MyFunction : REAL VAR_OUTPUT firstOutput : BOOL; END_VAR", "")
				.BindInterfaces();
			var myFunction = boundInterface.FunctionSymbols["MyFunction"].Type;
			Assert.Collection(myFunction.Parameters,
				p => { Assert.Equal(ParameterKind.Output, p.Kind); Assert.Equal("firstOutput".ToCaseInsensitive(), p.Name); },
				p => { Assert.Equal(ParameterKind.Output, p.Kind); Assert.Equal("MyFunction".ToCaseInsensitive(), p.Name); Assert.Equal(boundInterface.SystemScope.Real, p.Type); });
		}
		[Fact]
		public void Function_ExplicitReturnOutput()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("FUNCTION MyFunction VAR_OUTPUT MyFunction : BOOL; END_VAR", "")
				.BindInterfaces();
			var myFunction = boundInterface.FunctionSymbols["MyFunction"].Type;
			Assert.Collection(myFunction.Parameters,
				p => { Assert.Equal(ParameterKind.Output, p.Kind); Assert.Equal("MyFunction".ToCaseInsensitive(), p.Name); Assert.Equal(boundInterface.SystemScope.Bool, p.Type); });
		}
		[Fact]
		public void Function_ComplexTypeArg()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("TYPE MyEnum : (First := 1, Second := First); END_TYPE")
				.AddPou("FUNCTION MyFunction VAR_OUTPUT MyFunction : MyEnum; END_VAR", "")
				.BindInterfaces();
			var myEnum = boundInterface.Types["MyEnum"];
			var myFunction = boundInterface.FunctionSymbols["MyFunction"].Type;
			Assert.Collection(myFunction.Parameters,
				p => { Assert.Equal(ParameterKind.Output, p.Kind); Assert.Equal("MyFunction".ToCaseInsensitive(), p.Name); Assert.Equal(myEnum.Code, p.Type.Code); });
		}
		[Fact]
		public void Function_Error_DuplicateFunction()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("FUNCTION MyFunction", "")
				.AddPou("FUNCTION MyFunction", "")
				.BindInterfaces(ErrorOfType<SymbolAlreadyExistsMessage>(err => Assert.Equal("MyFunction", err.Name.Original)));
		}
		[Fact]
		public void Function_Error_DuplicateArg_SameKind()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("FUNCTION MyFunction VAR_INPUT a : INT; a : INT; END_VAR", "")
				.BindInterfaces(ErrorOfType<SymbolAlreadyExistsMessage>(err => Assert.Equal("a", err.Name.Original)));
		}
		[Fact]
		public void Function_Error_DuplicateArg_DiffrentKind()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("FUNCTION MyFunction VAR_OUTPUT a : INT; END_VAR VAR_INPUT a : INT; END_VAR", "")
				.BindInterfaces(ErrorOfType<SymbolAlreadyExistsMessage>(err => Assert.Equal("a", err.Name.Original)));
		}
		[Fact]
		public void Function_Error_Duplicate_ImplicitReturnVariable()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("FUNCTION MyFunction : REAL VAR_OUTPUT MyFunction : REAL; END_VAR", "")
				.BindInterfaces(ErrorOfType<SymbolAlreadyExistsMessage>(err => Assert.Equal("MyFunction", err.Name.Original)));
		}

	}
	public sealed class PouBindingTests
	{
		[Theory]
		[InlineData("FUNCTION", "VAR_INPUT")]
		[InlineData("FUNCTION", "VAR_OUTPUT")]
		[InlineData("FUNCTION", "VAR_IN_OUT")]
		[InlineData("FUNCTION_BLOCK", "VAR_INPUT")]
		[InlineData("FUNCTION_BLOCK", "VAR_OUTPUT")]
		[InlineData("FUNCTION_BLOCK", "VAR_IN_OUT")]
		[InlineData("FUNCTION_BLOCK", "VAR")]
		public void Error_VariableCannotHaveInitalValue(string pouKind, string varKind)
		{
			BindHelper.NewProject
				.AddPou($"{pouKind} foo {varKind} value : INT := 0; END_VAR", "")
				.BindBodies(ErrorOfType<VariableCannotHaveInitialValueMessage>());
		}

		[Theory]
		[InlineData("FUNCTION", "VAR")]
		[InlineData("FUNCTION", "VAR_TEMP")]
		[InlineData("FUNCTION_BLOCK", "VAR_TEMP")]
		public void InitialValue(string pouKind, string varKind)
		{
			BindHelper.NewProject
				.AddPou($"{pouKind} foo {varKind} value : INT := 6; END_VAR", "value;")
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
		[InlineData("FUNCTION", "VAR")]
		[InlineData("FUNCTION", "VAR_TEMP")]
		[InlineData("FUNCTION_BLOCK", "VAR_TEMP")]
		public void Error_InitialValue_WrongType(string pouKind, string varKind)
		{
			BindHelper.NewProject
				.AddPou($"{pouKind} foo {varKind} value : INT := BOOL#FALSE; END_VAR", "")
				.BindBodies(ErrorOfType<TypeIsNotConvertibleMessage>());
		}
		[Theory]
		[InlineData("FUNCTION", "VAR")]
		[InlineData("FUNCTION", "VAR_TEMP")]
		[InlineData("FUNCTION_BLOCK", "VAR_TEMP")]
		public void InitialValue_ReferenceInput(string pouKind, string varKind)
		{
			BindHelper.NewProject
				.AddPou($"{pouKind} foo VAR_INPUT inputArg : INT; END_VAR {varKind} value : INT := inputArg; END_VAR", "value;")
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
		[InlineData("FUNCTION", "VAR")]
		[InlineData("FUNCTION", "VAR_TEMP")]
		[InlineData("FUNCTION_BLOCK", "VAR_TEMP")]
		public void InitialValue_CannotReadTemp(string pouKind, string varKind)
		{
			BindHelper.NewProject
				.AddPou($"{pouKind} foo {varKind} otherTemp : INT; END_VAR {varKind} value : INT := otherTemp; END_VAR", "")
				.BindBodies(ErrorOfType<VariableNotFoundMessage>(msg => AssertEx.EqualCaseInsensitive("otherTemp", msg.Identifier)));
		}
	}
}
