using Compiler;
using Compiler.Messages;
using Xunit;

namespace Tests
{
	using static ErrorTestHelper;

	public sealed class ProgramBindingTests
	{
		private static readonly SystemScope SystemScope = BindHelper.SystemScope;
		[Fact]
		public void Empty()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("PROGRAM MyProgram", "")
				.BindInterfaces();
			var myProgram = boundInterface.FunctionSymbols["MyProgram"];
			Assert.Empty(myProgram.Parameters);
		}

		[Fact]
		public void WithInput()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("PROGRAM MyProgram VAR_INPUT myInput : INT; END_VAR", "")
				.BindInterfaces();
			var myProgram = boundInterface.FunctionSymbols["MyProgram"];
			Assert.Collection(myProgram.Parameters,
				p => { Assert.Equal(ParameterKind.Input, p.Kind); Assert.Equal("myInput".ToCaseInsensitive(), p.Name); Assert.Equal(SystemScope.Int, p.Type); });
		}

		[Fact]
		public void WithOutput()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("PROGRAM MyProgram VAR_OUTPUT myOutput : BOOL; END_VAR", "")
				.BindInterfaces();
			var myProgram = boundInterface.FunctionSymbols["MyProgram"];
			Assert.Collection(myProgram.Parameters,
				p => { Assert.Equal(ParameterKind.Output, p.Kind); Assert.Equal("myOutput".ToCaseInsensitive(), p.Name); Assert.Equal(SystemScope.Bool, p.Type); });
		}
		[Fact]
		public void WithInOut()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("PROGRAM MyProgram VAR_IN_OUT myInOut : REAL; END_VAR", "")
				.BindInterfaces();
			var myProgram = boundInterface.FunctionSymbols["MyProgram"];
			Assert.Collection(myProgram.Parameters,
				p => { Assert.Equal(ParameterKind.InOut, p.Kind); Assert.Equal("myInOut".ToCaseInsensitive(), p.Name); Assert.Equal(SystemScope.Real, p.Type); });
		}
		[Fact]
		public void TempIsIgnored()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("PROGRAM MyProgram VAR_TEMP myTemp : REAL; END_VAR", "")
				.BindInterfaces();
			var myProgram = boundInterface.FunctionSymbols["MyProgram"];
			Assert.Empty(myProgram.Parameters);
		}
		[Fact]
		public void VarIsIgnored()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("PROGRAM MyProgram VAR myStatic : REAL; END_VAR", "")
				.BindInterfaces();
			var myProgram = boundInterface.FunctionSymbols["MyProgram"];
			Assert.Empty(myProgram.Parameters);
		}
		[Fact]
		public void InputsInSameBlock()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("PROGRAM MyProgram VAR_INPUT input1 : REAL; input2 : INT; END_VAR", "")
				.BindInterfaces();
			var myProgram = boundInterface.FunctionSymbols["MyProgram"];
			Assert.Collection(myProgram.Parameters,
				p => Assert.Equal("input1".ToCaseInsensitive(), p.Name),
				p => Assert.Equal("input2".ToCaseInsensitive(), p.Name));
		}
		[Fact]
		public void InputsInDiffrentBlock()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("PROGRAM MyProgram VAR_INPUT input1 : REAL; END_VAR VAR_INPUT input2 : INT; END_VAR", "")
				.BindInterfaces();
			var myProgram = boundInterface.FunctionSymbols["MyProgram"];
			Assert.Collection(myProgram.Parameters,
				p => Assert.Equal("input1".ToCaseInsensitive(), p.Name),
				p => Assert.Equal("input2".ToCaseInsensitive(), p.Name));
		}
		[Fact]
		public void ReturnAsOutput()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("PROGRAM MyProgram : REAL VAR_OUTPUT firstOutput : BOOL; END_VAR", "")
				.BindInterfaces();
			var myProgram = boundInterface.FunctionSymbols["MyProgram"];
			Assert.Collection(myProgram.Parameters,
				p => { Assert.Equal(ParameterKind.Output, p.Kind); Assert.Equal("firstOutput".ToCaseInsensitive(), p.Name); },
				p => { Assert.Equal(ParameterKind.Output, p.Kind); Assert.Equal("MyProgram".ToCaseInsensitive(), p.Name); Assert.Equal(SystemScope.Real, p.Type); });
		}
		[Fact]
		public void ExplicitReturnOutput()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("PROGRAM MyProgram VAR_OUTPUT MyProgram : BOOL; END_VAR", "")
				.BindInterfaces();
			var myProgram = boundInterface.FunctionSymbols["MyProgram"];
			Assert.Collection(myProgram.Parameters,
				p => { Assert.Equal(ParameterKind.Output, p.Kind); Assert.Equal("MyProgram".ToCaseInsensitive(), p.Name); Assert.Equal(SystemScope.Bool, p.Type); });
		}
		[Fact]
		public void ComplexTypeArg()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("TYPE MyEnum : (First := 1, Second := First); END_TYPE")
				.AddPou("PROGRAM MyProgram VAR_OUTPUT MyProgram : MyEnum; END_VAR", "")
				.BindInterfaces();
			var myEnum = boundInterface.DutTypes["MyEnum"];
			var myProgram = boundInterface.FunctionSymbols["MyProgram"];
			Assert.Collection(myProgram.Parameters,
				p => { Assert.Equal(ParameterKind.Output, p.Kind); Assert.Equal("MyProgram".ToCaseInsensitive(), p.Name); Assert.Equal(myEnum.Code, p.Type.Code); });
		}
		[Fact]
		public void Error_Duplicate()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("PROGRAM MyProgram", "")
				.AddPou("PROGRAM MyProgram", "")
				.BindInterfaces(ErrorOfType<SymbolAlreadyExistsMessage>(err => Assert.Equal("MyProgram", err.Name.Original)));
		}
		[Fact]
		public void Error_DuplicateArg_SameKind()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("PROGRAM MyProgram VAR_INPUT a : INT; a : INT; END_VAR", "")
				.BindInterfaces(ErrorOfType<SymbolAlreadyExistsMessage>(err => Assert.Equal("a", err.Name.Original)));
		}
		[Fact]
		public void Error_DuplicateArg_DiffrentKind()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("PROGRAM MyProgram VAR_OUTPUT a : INT; END_VAR VAR_INPUT a : INT; END_VAR", "")
				.BindInterfaces(ErrorOfType<SymbolAlreadyExistsMessage>(err => Assert.Equal("a", err.Name.Original)));
		}
		[Fact]
		public void Error_Duplicate_ImplicitReturnVariable()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("PROGRAM MyProgram : REAL VAR_OUTPUT MyProgram : REAL; END_VAR", "")
				.BindInterfaces(ErrorOfType<SymbolAlreadyExistsMessage>(err => Assert.Equal("MyProgram", err.Name.Original)));
		}
	}
}
