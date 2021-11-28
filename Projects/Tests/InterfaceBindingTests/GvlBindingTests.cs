using Compiler;
using Compiler.Messages;
using System.Linq;
using Xunit;

namespace Tests
{
	using static ErrorTestHelper;

	public sealed class GvlBindingTests
	{
		[Fact]
		public void EmptyGvl()
		{
			var boundInterface = BindHelper.NewProject
				.AddGVL("MyGvl", "")
				.BindInterfaces();
			var myGvl = boundInterface.GlobalVariableListSymbols["MyGvl"];
			Assert.Empty(myGvl.Variables);
		}
		
		[Fact]
		public void GvlWithVariables()
		{
			var boundInterface = BindHelper.NewProject
				.AddGVL("MyOtherGVL", "VAR_GLOBAL xyz : INT; abc : REAL; END_VAR")
				.BindInterfaces();
			var myGvl = boundInterface.GlobalVariableListSymbols["MyOtherGVL"];
			Assert.Collection(myGvl.Variables.OrderBy(v => v.Name),
				v => AssertEx.CheckVariable(v, "abc", boundInterface.SystemScope.Real),
				v => AssertEx.CheckVariable(v, "xyz", boundInterface.SystemScope.Int));
		}
		
		[Fact]
		public void MultipleGvls()
		{
			var boundInterface = BindHelper.NewProject
				.AddGVL("MyGVL", "VAR_GLOBAL xyz : INT; END_VAR")
				.AddGVL("MyOtherGVL", "VAR_GLOBAL abc : REAL; END_VAR")
				.BindInterfaces();
			var myGvl = boundInterface.GlobalVariableListSymbols["myGvl"];
			Assert.Collection(myGvl.Variables.OrderBy(v => v.Name),
				v => AssertEx.CheckVariable(v, "xyz", boundInterface.SystemScope.Int));
			var myOtherGvl = boundInterface.GlobalVariableListSymbols["myOtherGVL"];
			Assert.Collection(myOtherGvl.Variables.OrderBy(v => v.Name),
				v => AssertEx.CheckVariable(v, "abc", boundInterface.SystemScope.Real));
		}
		[Fact]
		public void Multiple_VarGlobal_Blocks()
		{
			var boundInterface = BindHelper.NewProject
				.AddGVL("MyGVL", "VAR_GLOBAL xyz : INT; END_VAR VAR_GLOBAL abc : REAL; END_VAR")
				.BindInterfaces();
			var myGvl = boundInterface.GlobalVariableListSymbols["MyGVL"];
			Assert.Collection(myGvl.Variables.OrderBy(v => v.Name),
				v => AssertEx.CheckVariable(v, "abc", boundInterface.SystemScope.Real),
				v => AssertEx.CheckVariable(v, "xyz", boundInterface.SystemScope.Int));
		}
		[Fact]
		public void Error_GVL_VAR_Block()
		{
			BindHelper.NewProject
				.AddGVL("MyGVL", "VAR xyz : INT; END_VAR")
				.BindInterfaces(ErrorOfType<OnlyVarGlobalInGvlMessage>());
		}
		[Fact]
		public void Error_Duplicate_GVL()
		{
			BindHelper.NewProject
				.AddGVL("MyGVL", "VAR_GLOBAL xyz : INT; END_VAR")
				.AddGVL("MyGVL", "VAR_GLOBAL abc : INT; END_VAR")
				.BindInterfaces(ErrorOfType<SymbolAlreadyExistsMessage>(sym => Assert.Equal("MyGVL".ToCaseInsensitive(), sym.Name)));
		}
		[Fact]
		public void Error_Duplicate_Var()
		{
			BindHelper.NewProject
				.AddGVL("MyGVL", "VAR_GLOBAL xyz : INT; xyz : REAL; END_VAR")
				.BindInterfaces(ErrorOfType<SymbolAlreadyExistsMessage>(sym => Assert.Equal("xyz".ToCaseInsensitive(), sym.Name)));
		}
		[Fact]
		public void TypeLayoutInGvl()
		{
			var boundInterface = BindHelper.NewProject
				.AddGVL("MyGVL", "VAR_GLOBAL ptr : POINTER TO INT; END_VAR")
				.BindInterfaces();
			Assert.Equal(boundInterface.SystemScope.PointerSize, boundInterface.GlobalVariableListSymbols["MyGVL"].Variables["ptr"].Type.LayoutInfo.Size);
		}

		[Fact]
		public void Error_NameCollisionEnumGvl()
		{
			BindHelper.NewProject
				.AddGVL("MyGvl", "")
				.AddDutFast("MyGvl", "(First, Second)")
				.BindInterfaces(ErrorOfType<SymbolAlreadyExistsMessage>(err => Assert.Equal("MyGvl".ToCaseInsensitive(), err.Name)));
		}
	}
}
