using Compiler;
using Compiler.Messages;
using Compiler.Types;
using System.Collections.Immutable;
using Xunit;

namespace Tests
{
	using static ErrorTestHelper;

	public sealed class AliasBindingTests
	{
		private static readonly SystemScope SystemScope = BindHelper.SystemScope;
		[Fact]
		public void AliasToBuiltIn()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("TYPE myAlias : INT; END_TYPE")
				.BindInterfaces();
			var myAlias = Assert.IsType<AliasTypeSymbol>(boundInterface.DutTypes["myAlias"]);
			AssertEx.EqualType(SystemScope.Int, myAlias.AliasedType);
			Assert.Equal(SystemScope.Int.LayoutInfo, myAlias.LayoutInfo);
		}

		[Fact]
		public void AliasToArray()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("TYPE myAlias : ARRAY[0..10] OF INT; END_TYPE")
				.BindInterfaces();
			var myAlias = Assert.IsType<AliasTypeSymbol>(boundInterface.DutTypes["myAlias"]);
			var arrayType = new ArrayType(SystemScope.Int, ImmutableArray.Create(new ArrayType.Range(0, 10)));
			AssertEx.EqualType(arrayType, myAlias.AliasedType);
			Assert.Equal(arrayType.LayoutInfo, myAlias.LayoutInfo);
		}
		[Fact]
		public void AliasToStructure()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("TYPE myStruct : STRUCT field : BOOL; END_STRUCT; END_TYPE")
				.AddDut("TYPE myAlias : myStruct; END_TYPE")
				.BindInterfaces();
			var myAlias = Assert.IsType<AliasTypeSymbol>(boundInterface.DutTypes["myAlias"]);
			var structType = boundInterface.DutTypes["myStruct"];
			AssertEx.EqualType(structType, myAlias.AliasedType);
			Assert.Equal(structType.LayoutInfo, myAlias.LayoutInfo);
		}
		[Fact]
		public void AliasToAlias()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("TYPE myAlias1 : INT; END_TYPE")
				.AddDut("TYPE myAlias2 : myAlias1; END_TYPE")
				.BindInterfaces();
			var myAlias2 = Assert.IsType<AliasTypeSymbol>(boundInterface.DutTypes["myAlias2"]);
			var myAlias1 = boundInterface.DutTypes["myAlias1"];
			AssertEx.EqualType(myAlias1, myAlias2.AliasedType);
			Assert.Equal(SystemScope.Int.LayoutInfo, myAlias1.LayoutInfo);
			Assert.Equal(myAlias1.LayoutInfo, myAlias2.LayoutInfo);
		}
		[Fact]
		public void Error_RecursiveAlias()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("TYPE myAlias : myAlias; END_TYPE")
				.BindInterfaces(ErrorOfType<TypeNotCompleteMessage>());
			var myAlias = Assert.IsType<AliasTypeSymbol>(boundInterface.DutTypes["myAlias"]);
			AssertEx.EqualType(myAlias, myAlias.AliasedType);
		}
	}
}
