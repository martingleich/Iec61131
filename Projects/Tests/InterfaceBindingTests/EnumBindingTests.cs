using Compiler;
using Compiler.Messages;
using Compiler.Types;
using System.Linq;
using Xunit;

namespace Tests
{
	using static ErrorTestHelper;

	public sealed class EnumBindingTests
	{
		private static readonly SystemScope SystemScope = BindHelper.SystemScope;
		[Fact]
		public void EmptyEnum()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("TYPE MyEnum : (); END_TYPE")
				.BindInterfaces();

			var myEnum = Assert.IsType<EnumTypeSymbol>(boundInterface.DutTypes["MyEnum"]);
			Assert.Empty(myEnum.Values);
			Assert.Equal(SystemScope.Int, myEnum.BaseType);
		}

		[Fact]
		public void FullyInitialisedEnum()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("TYPE MyEnum : (First := 1, Second := 2); END_TYPE")
				.BindInterfaces();

			var myEnum = Assert.IsType<EnumTypeSymbol>(boundInterface.DutTypes["MyEnum"]);
			Assert.Collection(myEnum.Values.OrderBy(e => e.DeclaringPosition.Start),
				first => { Assert.Equal("First", first.Name.Original); Assert.Equal(1, Assert.IsType<IntLiteralValue>(first.Value.InnerValue).Value); },
				second => { Assert.Equal("Second", second.Name.Original); Assert.Equal(2, Assert.IsType<IntLiteralValue>(second.Value.InnerValue).Value); }
				);
			Assert.Equal(SystemScope.Int, myEnum.BaseType);
		}

		[Fact]
		public void AutoInitialisedEnum()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("TYPE MyEnum : (First, Second); END_TYPE")
				.BindInterfaces();

			var myEnum = Assert.IsType<EnumTypeSymbol>(boundInterface.DutTypes["MyEnum"]);
			Assert.Collection(myEnum.Values.OrderBy(e => e.DeclaringPosition.Start),
				first => { Assert.Equal("First", first.Name.Original); Assert.Equal(0, Assert.IsType<IntLiteralValue>(first.Value.InnerValue).Value); },
				second => { Assert.Equal("Second", second.Name.Original); Assert.Equal(1, Assert.IsType<IntLiteralValue>(second.Value.InnerValue).Value); }
				);
			Assert.Equal(SystemScope.Int, myEnum.BaseType);
		}

		[Fact]
		public void ReferenceOtherEnumValues()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("TYPE MyEnum : (First := 1, Second := First); END_TYPE")
				.BindInterfaces();

			var myEnum = Assert.IsType<EnumTypeSymbol>(boundInterface.DutTypes["MyEnum"]);
			Assert.Collection(myEnum.Values.OrderBy(e => e.DeclaringPosition.Start),
				first => { Assert.Equal("First", first.Name.Original); Assert.Equal(1, Assert.IsType<IntLiteralValue>(first.Value.InnerValue).Value); },
				second => { Assert.Equal("Second", second.Name.Original); Assert.Equal(1, Assert.IsType<IntLiteralValue>(second.Value.InnerValue).Value); }
				);
			Assert.Equal(SystemScope.Int, myEnum.BaseType);
		}

		[Fact]
		public void Error_RecursiveEnumDeclaration()
		{
			BindHelper.NewProject
				.AddDut("TYPE MyEnum : (First := Second, Second := First); END_TYPE")
				.BindInterfaces(ErrorOfType<RecursiveConstantDeclarationMessage>());
		}
	}
}
