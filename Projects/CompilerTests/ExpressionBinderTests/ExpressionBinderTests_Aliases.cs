﻿using Compiler;
using Xunit;

namespace Tests.ExpressionBinderTests
{
	public static class ExpressionBinderTests_Aliases
	{
		[Fact]
		public static void LiteralAsAlias_SInt()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("myalias", "SINT")
				.BindGlobalExpression("5", "myalias");
			var cast = Assert.IsType<ImplicitAliasFromBaseTypeCastBoundExpression>(boundExpression);
			Assert.IsType<LiteralBoundExpression>(cast.Value);
			Assert.Equal("myalias", boundExpression.Type.Code);
		}
		[Fact]
		public static void LiteralAsAlias_REAL()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("myalias", "REAL")
				.BindGlobalExpression("3.14", "myalias");
			var cast = Assert.IsType<ImplicitAliasFromBaseTypeCastBoundExpression>(boundExpression);
			Assert.IsType<LiteralBoundExpression>(cast.Value);
			Assert.Equal("myalias", boundExpression.Type.Code);
		}

		[Fact]
		public static void Addition_Int_AliasToInt()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("myalias", "INT")
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
				.AddDut("myalias", "INT")
				.WithGlobalVar("x", "myalias")
				.WithGlobalVar("y", "myalias")
				.BindGlobalExpression("x + y", null);
			Assert.Equal("myalias", boundExpression.Type.Code);
		}
		[Fact]
		public static void Addition_AliasToInt_AliasToInt_Diffrent()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("myalias1", "INT")
				.AddDut("myalias2", "INT")
				.WithGlobalVar("x", "myalias1")
				.WithGlobalVar("y", "myalias2")
				.BindGlobalExpression("x + y", null);
			Assert.Equal("Int", boundExpression.Type.Code);
		}
		[Fact]
		public static void Addition_AliasToPointerOffset()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("myalias", "POINTER TO INT")
				.WithGlobalVar("x", "myalias")
				.BindGlobalExpression("x + 5", null);
			Assert.Equal("myalias", boundExpression.Type.Code);
		}
		[Fact]
		public static void Addition_AliasToPointerOffset2()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("myalias", "POINTER TO INT")
				.WithGlobalVar("x", "myalias")
				.BindGlobalExpression("5 + x", null);
			Assert.Equal("myalias", boundExpression.Type.Code);
		}
		[Fact]
		public static void Subtraction_DiffrentPointerAliases()
		{
			BindHelper.NewProject
				.AddDut("myalias1", "POINTER TO INT")
				.AddDut("myalias2", "POINTER TO INT")
				.WithGlobalVar("x", "myalias1")
				.WithGlobalVar("y", "myalias2")
				.BindGlobalExpression("x - y", null);
		}
		[Fact]
		public static void ZeroAsAliasToPointer()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("myalias", "POINTER TO INT")
				.BindGlobalExpression("0", "myAlias");
			Assert.Equal("myalias", boundExpression.Type.Code);
		}
		[Fact]
		public static void UnaryOperator_On_Alias_Not()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("myalias", "BOOL")
				.WithGlobalVar("x", "myalias")
				.BindGlobalExpression("NOT x", null);
			Assert.Equal("myalias", boundExpression.Type.Code);
		}
		[Fact]
		public static void DerefAliasPointer()
		{
			BindHelper.NewProject
				.AddDut("myalias", "POINTER TO LINT")
				.WithGlobalVar("x", "myalias")
				.BindGlobalExpression("x^", null);
		}
		[Fact]
		public static void SizeofAlias()
		{
			var (boundExpression, boundItf) = BindHelper.NewProject
				.AddDut("myalias", "LINT")
				.BindGlobalExpressionEx<SizeOfTypeBoundExpression>("SIZEOF(myalias)", null);
			AssertEx.HasConstantValue(boundExpression, boundItf.SystemScope, value =>
				Assert.Equal(8, Assert.IsType<IntLiteralValue>(value).Value));
		}
		[Fact]
		public static void Casting_AliasToBase()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("mydut", "STRUCT field1 : INT; END_STRUCT")
				.AddDut("myalias", "mydut")
				.WithGlobalVar("x", "myalias")
				.BindGlobalExpression("x", "mydut");
			Assert.IsType<ImplicitAliasToBaseTypeCastBoundExpression>(boundExpression);
			Assert.Equal("mydut", boundExpression.Type.Code);
		}
		[Fact]
		public static void Casting_BaseToAlias()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("mydut", "STRUCT field1 : INT; END_STRUCT")
				.AddDut("myalias", "mydut")
				.WithGlobalVar("x", "mydut")
				.BindGlobalExpression("x", "myalias");
			Assert.IsType<ImplicitAliasFromBaseTypeCastBoundExpression>(boundExpression);
			Assert.Equal("myalias", boundExpression.Type.Code);
		}
		[Fact]
		public static void Casting_AliasToAlias_Diffrent()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("mydut", "STRUCT field1 : INT; END_STRUCT")
				.AddDut("myalias1", "mydut")
				.AddDut("myalias2", "mydut")
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
				.AddDut("mydut", "STRUCT field1 : INT; END_STRUCT")
				.AddDut("myalias", "mydut")
				.WithGlobalVar("x", "myalias")
				.BindGlobalExpression("x", "myalias");
			var variable = Assert.IsType<VariableBoundExpression>(boundExpression);
			Assert.Equal("x", variable.Variable.Name.Original);
		}
	}

}
