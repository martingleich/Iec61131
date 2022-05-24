using Compiler;
using Compiler.Messages;
using Compiler.Types;
using Xunit;

namespace CompilerTests
{
	using static ErrorHelper;

	public sealed class FunctionBlockBindingTests
	{
		[Fact]
		public void Empty()
		{
			var boundInterface = BindHelper.NewProject
				.AddFunctionBlock("MyFb", "", "")
				.BindInterfaces();
			var myFb = Assert.IsType<FunctionBlockSymbol>(boundInterface.Types["MyFb"]);
			Assert.Empty(myFb.Parameters);
			Assert.Empty(myFb.Fields);
		}
		#region Same as function
		[Fact]
		public void Function_WithInput()
		{
			var boundInterface = BindHelper.NewProject
				.AddFunctionBlock("MyFunction", "VAR_INPUT myInput : INT; END_VAR", "")
				.BindInterfaces();
			var myFunction = Assert.IsType<FunctionBlockSymbol>(boundInterface.Types["MyFunction"]);
			Assert.Collection(myFunction.Parameters,
				p => { Assert.Equal(ParameterKind.Input, p.Kind); Assert.Equal("myInput".ToCaseInsensitive(), p.Name); Assert.Equal(boundInterface.SystemScope.Int, p.Type); });
		}

		[Fact]
		public void Function_WithOutput()
		{
			var boundInterface = BindHelper.NewProject
				.AddFunctionBlock("MyFunction", "VAR_OUTPUT myOutput : BOOL; END_VAR", "")
				.BindInterfaces();
			var myFunction = Assert.IsType<FunctionBlockSymbol>(boundInterface.Types["MyFunction"]);
			Assert.Collection(myFunction.Parameters,
				p => { Assert.Equal(ParameterKind.Output, p.Kind); Assert.Equal("myOutput".ToCaseInsensitive(), p.Name); Assert.Equal(boundInterface.SystemScope.Bool, p.Type); });
		}
		[Fact]
		public void Function_WithInOut()
		{
			var boundInterface = BindHelper.NewProject
				.AddFunctionBlock("MyFunction", "VAR_IN_OUT myInOut : REAL; END_VAR", "")
				.BindInterfaces();
			var myFunction = Assert.IsType<FunctionBlockSymbol>(boundInterface.Types["MyFunction"]);
			Assert.Collection(myFunction.Parameters,
				p => { Assert.Equal(ParameterKind.InOut, p.Kind); Assert.Equal("myInOut".ToCaseInsensitive(), p.Name); Assert.Equal(boundInterface.SystemScope.Real, p.Type); });
		}
		[Fact]
		public void Function_TempIsIgnored()
		{
			var boundInterface = BindHelper.NewProject
				.AddFunctionBlock("MyFunction", "VAR_TEMP myTemp : REAL; END_VAR", "")
				.BindInterfaces();
			var myFunction = Assert.IsType<FunctionBlockSymbol>(boundInterface.Types["MyFunction"]);
			Assert.Empty(myFunction.Parameters);
		}
		[Fact]
		public void Function_VarInstIsIgnored()
		{
			var boundInterface = BindHelper.NewProject
				.AddFunctionBlock("MyFunction", "VAR_INST myTemp : REAL; END_VAR", "")
				.BindInterfaces();
			var myFunction = Assert.IsType<FunctionBlockSymbol>(boundInterface.Types["MyFunction"]);
			Assert.Empty(myFunction.Parameters);
		}
		[Fact]
		public void Function_InputsInSameBlock()
		{
			var boundInterface = BindHelper.NewProject
				.AddFunctionBlock("MyFunction", "VAR_INPUT input1 : REAL; input2 : INT; END_VAR", "")
				.BindInterfaces();
			var myFunction = Assert.IsType<FunctionBlockSymbol>(boundInterface.Types["MyFunction"]);
			Assert.Collection(myFunction.Parameters,
				p => Assert.Equal("input1".ToCaseInsensitive(), p.Name),
				p => Assert.Equal("input2".ToCaseInsensitive(), p.Name));
		}
		[Fact]
		public void Function_InputsInDiffrentBlock()
		{
			var boundInterface = BindHelper.NewProject
				.AddFunctionBlock("MyFunction", "VAR_INPUT input1 : REAL; END_VAR VAR_INPUT input2 : INT; END_VAR", "")
				.BindInterfaces();
			var myFunction = Assert.IsType<FunctionBlockSymbol>(boundInterface.Types["MyFunction"]);
			Assert.Collection(myFunction.Parameters,
				p => Assert.Equal("input1".ToCaseInsensitive(), p.Name),
				p => Assert.Equal("input2".ToCaseInsensitive(), p.Name));
		}
		[Fact]
		public void Function_ReturnAsOutput()
		{
			var boundInterface = BindHelper.NewProject
				.AddFunctionBlock("MyFunction", ": REAL VAR_OUTPUT firstOutput : BOOL; END_VAR", "")
				.BindInterfaces();
			var myFunction = Assert.IsType<FunctionBlockSymbol>(boundInterface.Types["MyFunction"]);
			Assert.Collection(myFunction.Parameters,
				p => { Assert.Equal(ParameterKind.Output, p.Kind); Assert.Equal("firstOutput".ToCaseInsensitive(), p.Name); },
				p => { Assert.Equal(ParameterKind.Output, p.Kind); Assert.Equal("MyFunction".ToCaseInsensitive(), p.Name); Assert.Equal(boundInterface.SystemScope.Real, p.Type); });
		}
		[Fact]
		public void Function_ExplicitReturnOutput()
		{
			var boundInterface = BindHelper.NewProject
				.AddFunctionBlock("MyFunction", "VAR_OUTPUT MyFunction : BOOL; END_VAR", "")
				.BindInterfaces();
			var myFunction = Assert.IsType<FunctionBlockSymbol>(boundInterface.Types["MyFunction"]);
			Assert.Collection(myFunction.Parameters,
				p => { Assert.Equal(ParameterKind.Output, p.Kind); Assert.Equal("MyFunction".ToCaseInsensitive(), p.Name); Assert.Equal(boundInterface.SystemScope.Bool, p.Type); });
		}
		[Fact]
		public void Function_ComplexTypeArg()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("MyEnum", "(First := 1, Second := First)")
				.AddFunctionBlock("MyFunction", "VAR_OUTPUT MyFunction : MyEnum; END_VAR", "")
				.BindInterfaces();
			var myEnum = boundInterface.Types["MyEnum"];
			var myFunction = Assert.IsType<FunctionBlockSymbol>(boundInterface.Types["MyFunction"]);
			Assert.Collection(myFunction.Parameters,
				p => { Assert.Equal(ParameterKind.Output, p.Kind); Assert.Equal("MyFunction".ToCaseInsensitive(), p.Name); Assert.Equal(myEnum.Code, p.Type.Code); });
		}
		[Fact]
		public void Function_Error_DuplicateFunction()
		{
			var boundInterface = BindHelper.NewProject
				.AddFunctionBlock("MyFunction", "", "")
				.AddFunctionBlock("MyFunction", "", "")
				.BindInterfaces(ErrorOfType<SymbolAlreadyExistsMessage>(err => Assert.Equal("MyFunction", err.Name.Original)));
		}
		[Fact]
		public void Function_Error_DuplicateArg_SameKind()
		{
			var boundInterface = BindHelper.NewProject
				.AddFunctionBlock("MyFunction", "VAR_INPUT a : INT; a : INT; END_VAR", "")
				.BindInterfaces(ErrorOfType<SymbolAlreadyExistsMessage>(err => Assert.Equal("a", err.Name.Original)));
		}
		[Fact]
		public void Function_Error_DuplicateArg_DiffrentKind()
		{
			var boundInterface = BindHelper.NewProject
				.AddFunctionBlock("MyFunction", "VAR_OUTPUT a : INT; END_VAR VAR_INPUT a : INT; END_VAR", "")
				.BindInterfaces(ErrorOfType<SymbolAlreadyExistsMessage>(err => Assert.Equal("a", err.Name.Original)));
		}
		[Fact]
		public void Function_Error_Duplicate_ImplicitReturnVariable()
		{
			var boundInterface = BindHelper.NewProject
				.AddFunctionBlock("MyFunction", ": REAL VAR_OUTPUT MyFunction : REAL; END_VAR", "")
				.BindInterfaces(ErrorOfType<SymbolAlreadyExistsMessage>(err => Assert.Equal("MyFunction", err.Name.Original)));
		}
		#endregion
		[Fact]
		public void MemberVariable()
		{
			var boundInterface = BindHelper.NewProject
				.AddFunctionBlock("MyFunction", "VAR_INST field : REAL; END_VAR", "")
				.BindInterfaces();
			var fb = Assert.IsType<FunctionBlockSymbol>(boundInterface.Types["MyFunction"]);
			Assert.Collection(fb.Fields,
				f => { Assert.Equal("field", f.Name.Original); });
		}
		[Fact]
		public void Error_MemberVariable_CollideParameter()
		{
			var boundInterface = BindHelper.NewProject
				.AddFunctionBlock("MyFunction", "VAR_INPUT field : INT; END_VAR VAR_INST field : REAL; END_VAR", "")
				.BindInterfaces(ErrorOfType<SymbolAlreadyExistsMessage>(err => Assert.Equal("field", err.Name.Original)));
		}
	}
}
