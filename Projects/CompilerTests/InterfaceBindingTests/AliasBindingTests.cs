using Compiler;
using Compiler.Messages;
using Compiler.Types;
using Runtime.IR.RuntimeTypes;
using System.Collections.Immutable;
using Xunit;

namespace CompilerTests
{
	using static ErrorHelper;

	public sealed class AliasBindingTests
	{
		[Fact]
		public void AliasToBuiltIn()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("myAlias", "INT")
				.BindInterfaces();
			var myAlias = Assert.IsType<AliasTypeSymbol>(boundInterface.Types["myAlias"]);
			AssertEx.EqualType(boundInterface.SystemScope.Int, myAlias.AliasedType);
			Assert.Equal(boundInterface.SystemScope.Int.LayoutInfo, myAlias.LayoutInfo);
		}

		[Fact]
		public void AliasToArray()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("myAlias", "ARRAY[0..10] OF INT")
				.BindInterfaces();
			var myAlias = Assert.IsType<AliasTypeSymbol>(boundInterface.Types["myAlias"]);
			var arrayType = new ArrayType(boundInterface.SystemScope.Int, ImmutableArray.Create(new ArrayTypeRange(0, 10)));
			AssertEx.EqualType(arrayType, myAlias.AliasedType);
			Assert.Equal(arrayType.LayoutInfo, myAlias.LayoutInfo);
		}
		[Fact]
		public void AliasToStructure()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("myStruct", "STRUCT field : BOOL; END_STRUCT")
				.AddDut("myAlias", "myStruct")
				.BindInterfaces();
			var myAlias = Assert.IsType<AliasTypeSymbol>(boundInterface.Types["myAlias"]);
			var structType = boundInterface.Types["myStruct"];
			AssertEx.EqualType(structType, myAlias.AliasedType);
			Assert.Equal(structType.LayoutInfo, myAlias.LayoutInfo);
		}
		[Fact]
		public void AliasToAlias()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("myAlias1", "INT")
				.AddDut("myAlias2", "myAlias1")
				.BindInterfaces();
			var myAlias2 = Assert.IsType<AliasTypeSymbol>(boundInterface.Types["myAlias2"]);
			var myAlias1 = boundInterface.Types["myAlias1"];
			AssertEx.EqualType(myAlias1, myAlias2.AliasedType);
			Assert.Equal(boundInterface.SystemScope.Int.LayoutInfo, myAlias1.LayoutInfo);
			Assert.Equal(myAlias1.LayoutInfo, myAlias2.LayoutInfo);
		}
		[Fact]
		public void Error_RecursiveAlias()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("myAlias", "myAlias")
				.BindInterfaces(ErrorOfType<TypeNotCompleteMessage>());
			var myAlias = Assert.IsType<AliasTypeSymbol>(boundInterface.Types["myAlias"]);
			AssertEx.EqualType(myAlias, myAlias.AliasedType);
		}
	}
}
