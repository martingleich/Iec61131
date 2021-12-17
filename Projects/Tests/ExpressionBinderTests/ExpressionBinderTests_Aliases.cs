using Compiler;
using Xunit;

namespace Tests.ExpressionBinderTests
{
	public static class ExpressionBinderTests_Aliases
	{
		private static readonly SystemScope SystemScope = BindHelper.SystemScope;
		[Fact]
		public static void LiteralAsAlias_SInt()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("TYPE myalias : SINT; END_TYPE")
				.BindGlobalExpression("5", "myalias");
			var cast = Assert.IsType<ImplicitAliasFromBaseTypeCastBoundExpression>(boundExpression);
			Assert.IsType<LiteralBoundExpression>(cast.Value);
			Assert.Equal("myalias", boundExpression.Type.Code);
		}
		[Fact]
		public static void LiteralAsAlias_REAL()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("TYPE myalias : REAL; END_TYPE")
				.BindGlobalExpression("3.14", "myalias");
			var cast = Assert.IsType<ImplicitAliasFromBaseTypeCastBoundExpression>(boundExpression);
			Assert.IsType<LiteralBoundExpression>(cast.Value);
			Assert.Equal("myalias", boundExpression.Type.Code);
		}

		[Fact]
		public static void Addition_Int_AliasToInt()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("TYPE myalias : INT; END_TYPE")
				.WithGlobalVar("x", "myalias")
				.BindGlobalExpression("1 + x", null);
			var op = Assert.IsType<BinaryOperatorBoundExpression>(boundExpression);
			Assert.IsType<ImplicitAliasToBaseTypeCastBoundExpression>(op.Right);
			Assert.Equal("Int", boundExpression.Type.Code);
		}
		[Fact]
		public static void Addition_AliasToInt_AliasToInt_Same()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("TYPE myalias : INT; END_TYPE")
				.WithGlobalVar("x", "myalias")
				.WithGlobalVar("y", "myalias")
				.BindGlobalExpression("x + y", null);
			Assert.Equal("myalias", boundExpression.Type.Code);
		}
		[Fact]
		public static void Addition_AliasToInt_AliasToInt_Diffrent()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("TYPE myalias1 : INT; END_TYPE")
				.AddDut("TYPE myalias2 : INT; END_TYPE")
				.WithGlobalVar("x", "myalias1")
				.WithGlobalVar("y", "myalias2")
				.BindGlobalExpression("x + y", null);
			Assert.Equal("Int", boundExpression.Type.Code);
		}
		[Fact]
		public static void Addition_AliasToPointerOffset()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("TYPE myalias : POINTER TO INT; END_TYPE")
				.WithGlobalVar("x", "myalias")
				.BindGlobalExpression("x + 5", null);
			Assert.Equal("myalias", boundExpression.Type.Code);
		}
		[Fact]
		public static void Addition_AliasToPointerOffset2()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("TYPE myalias : POINTER TO INT; END_TYPE")
				.WithGlobalVar("x", "myalias")
				.BindGlobalExpression("5 + x", null);
			Assert.Equal("myalias", boundExpression.Type.Code);
		}
		[Fact]
		public static void Subtraction_DiffrentPointerAliases()
		{
			BindHelper.NewProject
				.AddDut("TYPE myalias1 : POINTER TO INT; END_TYPE")
				.AddDut("TYPE myalias2 : POINTER TO INT; END_TYPE")
				.WithGlobalVar("x", "myalias1")
				.WithGlobalVar("y", "myalias2")
				.BindGlobalExpression("x - y", null);
		}
		[Fact]
		public static void ZeroAsAliasToPointer()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("TYPE myalias : POINTER TO INT; END_TYPE")
				.BindGlobalExpression("0", "myAlias");
			Assert.Equal("myalias", boundExpression.Type.Code);
		}
		[Fact]
		public static void UnaryOperator_On_Alias_Not()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("TYPE myalias : BOOL; END_TYPE")
				.WithGlobalVar("x", "myalias")
				.BindGlobalExpression("NOT x", null);
			Assert.Equal("myalias", boundExpression.Type.Code);
		}
		[Fact]
		public static void DerefAliasPointer()
		{
			BindHelper.NewProject
				.AddDut("TYPE myalias : POINTER TO LINT; END_TYPE")
				.WithGlobalVar("x", "myalias")
				.BindGlobalExpression("x^", null);
		}
		[Fact]
		public static void SizeofAlias()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("TYPE myalias : LINT; END_TYPE")
				.BindGlobalExpression("SIZEOF(myalias)", null);
			Assert.IsType<SizeOfTypeBoundExpression>(boundExpression);
			AssertEx.HasConstantValue(boundExpression, SystemScope, value =>
				Assert.Equal(8, Assert.IsType<IntLiteralValue>(value).Value));
		}
		[Fact]
		public static void Casting_AliasToBase()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("TYPE mydut : STRUCT field1 : INT; END_STRUCT; END_TYPE")
				.AddDut("TYPE myalias : mydut; END_TYPE")
				.WithGlobalVar("x", "myalias")
				.BindGlobalExpression("x", "mydut");
			Assert.IsType<ImplicitAliasToBaseTypeCastBoundExpression>(boundExpression);
			Assert.Equal("mydut", boundExpression.Type.Code);
		}
		[Fact]
		public static void Casting_BaseToAlias()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("TYPE mydut : STRUCT field1 : INT; END_STRUCT; END_TYPE")
				.AddDut("TYPE myalias : mydut; END_TYPE")
				.WithGlobalVar("x", "mydut")
				.BindGlobalExpression("x", "myalias");
			Assert.IsType<ImplicitAliasFromBaseTypeCastBoundExpression>(boundExpression);
			Assert.Equal("myalias", boundExpression.Type.Code);
		}
		[Fact]
		public static void Casting_AliasToAlias_Diffrent()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("TYPE mydut : STRUCT field1 : INT; END_STRUCT; END_TYPE")
				.AddDut("TYPE myalias1 : mydut; END_TYPE")
				.AddDut("TYPE myalias2 : mydut; END_TYPE")
				.WithGlobalVar("x", "myalias1")
				.BindGlobalExpression("x", "myalias2");
			var cast1 = Assert.IsType<ImplicitAliasFromBaseTypeCastBoundExpression>(boundExpression);
			Assert.Equal("myalias2", cast1.Type.Code);
			var cast2 = Assert.IsType<ImplicitAliasToBaseTypeCastBoundExpression>(cast1.Value);
			Assert.Equal("mydut", cast2.Type.Code);
		}
		[Fact]
		public static void Casting_AliasToAlias_Same()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("TYPE mydut : STRUCT field1 : INT; END_STRUCT; END_TYPE")
				.AddDut("TYPE myalias : mydut; END_TYPE")
				.WithGlobalVar("x", "myalias")
				.BindGlobalExpression("x", "myalias");
			var variable = Assert.IsType<VariableBoundExpression>(boundExpression);
			Assert.Equal("x", variable.Variable.Name.Original);
		}
	}

}
