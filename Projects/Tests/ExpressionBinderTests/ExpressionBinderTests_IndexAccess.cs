using Compiler;
using Compiler.Messages;
using Xunit;

namespace Tests.ExpressionBinderTests
{
	using static ErrorHelper;

	public static class ExpressionBinderTests_IndexAccess
	{
		[Theory]
		[InlineData("SINT")]
		[InlineData("USINT")]
		[InlineData("INT")]
		[InlineData("UINT")]
		[InlineData("DINT")]
		[InlineData("UDINT")]
		public static void IndexAccessToArray_VariousTypes(string indexType)
		{
			var boundExpression = BindHelper.NewProject
				.WithGlobalVar("arr", "ARRAY[0..10] OF INT")
				.WithGlobalVar("x", indexType)
				.BindGlobalExpression("arr[x]", null);
			Assert.IsType<ArrayIndexAccessBoundExpression>(boundExpression);
			AssertEx.EqualType("INT", boundExpression.Type);
		}

		[Fact]
		public static void MultipleIndexAccessToArray_2MixedIndex()
		{
			var boundExpression = BindHelper.NewProject
				.WithGlobalVar("arr", "ARRAY[0..10, 1..2] OF USINT")
				.WithGlobalVar("x", "INT")
				.WithGlobalVar("y", "SINT")
				.BindGlobalExpression("arr[x, y]", null);
			Assert.IsType<ArrayIndexAccessBoundExpression>(boundExpression);
			AssertEx.EqualType("USINT", boundExpression.Type);
		}
		[Fact]
		public static void MultipleIndexAccessToArray3_MixedIndex()
		{
			BindHelper.NewProject
				.WithGlobalVar("arr", "ARRAY[0..10, 1..2, -7..2] OF LREAL")
				.WithGlobalVar("x", "INT")
				.WithGlobalVar("y", "SINT")
				.WithGlobalVar("z", "DINT")
				.BindGlobalExpression("arr[x, y, z]", null);
		}

		[Fact]
		public static void IndexAccessCasted()
		{
			var boundExpression = BindHelper.NewProject
				.WithGlobalVar("arr", "ARRAY[0..10] OF SINT")
				.BindGlobalExpression("arr[1]", "INT");
			Assert.IsType<ImplicitCastBoundExpression>(boundExpression);
			AssertEx.EqualType("INT", boundExpression.Type);
		}
		[Fact]
		public static void IndexAccessDut()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("myDut", "STRUCT field : REAL; END_STRUCT")
				.WithGlobalVar("arr", "ARRAY[0..10] OF myDut")
				.BindGlobalExpression("arr[1]", null);
			AssertEx.EqualType("myDut", boundExpression.Type);
		}

		[Fact]
		public static void IndexAccessPointer()
		{
			var boundExpression = BindHelper.NewProject
				.WithGlobalVar("ptr", "POINTER TO LINT")
				.BindGlobalExpression("ptr[1]", null);
			Assert.IsType<PointerIndexAccessBoundExpression>(boundExpression);
			AssertEx.EqualType("LINT", boundExpression.Type);
		}
		[Fact]
		public static void IndexAccess_ToAliasOfArray()
		{
			BindHelper.NewProject
				.AddDut("myalias", "ARRAY[0..10] OF INT")
				.WithGlobalVar("arr", "myalias")
				.BindGlobalExpression("arr[0]", null);
		}
		[Fact]
		public static void IndexAccess_ToAliasOfPointer()
		{
			BindHelper.NewProject
				.AddDut("myalias", "POINTER TO INT")
				.WithGlobalVar("arr", "myalias")
				.BindGlobalExpression("arr[0]", null);
		}
		[Fact]
		public static void IndexAccess_WithAliasToInt()
		{
			BindHelper.NewProject
				.AddDut("myalias", "INT")
				.WithGlobalVar("arr", "ARRAY[0..5] OF BYTE")
				.WithGlobalVar("x", "myalias")
				.BindGlobalExpression("arr[x]", null);
		}

		[Fact]
		public static void IndexAccess_ArrayOfArray()
		{
			BindHelper.NewProject
				.WithGlobalVar("arr", "ARRAY[0..5] OF ARRAY[1..9] OF INT")
				.BindGlobalExpression("arr[4][2]", null);
		}

		[Fact]
		public static void Error_IndexAccess_ToNonArray()
		{
			BindHelper.NewProject
				.WithGlobalVar("arr", "LINT")
				.WithGlobalVar("x", "INT")
				.BindGlobalExpression("arr[x]", null, ErrorOfType<CannotIndexTypeMessage>());
		}

		[Fact]
		public static void Error_IndexAccess_WithNonIntegralTypeArray()
		{
			BindHelper.NewProject
				.WithGlobalVar("arr", "ARRAY[0..10] OF INT")
				.WithGlobalVar("x", "REAL")
				.BindGlobalExpression("arr[x]", null, ErrorOfType<TypeIsNotConvertibleMessage>());
		}
		[Fact]
		public static void Error_PointerIndexAccess_WithNonIntegralTypeArray()
		{
			BindHelper.NewProject
				.WithGlobalVar("arr", "POINTER TO INT")
				.WithGlobalVar("x", "BOOL")
				.BindGlobalExpression("arr[x]", null, ErrorOfType<TypeIsNotConvertibleMessage>());
		}
		[Fact]
		public static void Error_IndexAccessToFewDimensions()
		{
			BindHelper.NewProject
				.WithGlobalVar("arr", "ARRAY[0..10, 0..5] OF INT")
				.BindGlobalExpression("arr[4]", null, ErrorOfType<WrongNumberOfDimensionInIndexMessage>());
		}
		[Fact]
		public static void Error_IndexAccessToManyDimensions()
		{
			BindHelper.NewProject
				.WithGlobalVar("arr", "ARRAY[0..10] OF INT")
				.BindGlobalExpression("arr[4, 9]", null, ErrorOfType<WrongNumberOfDimensionInIndexMessage>());
		}
		[Fact]
		public static void Error_PointerIndexAccessToManyDimensions()
		{
			BindHelper.NewProject
				.WithGlobalVar("arr", "POINTER TO INT")
				.BindGlobalExpression("arr[4, 9]", null, ErrorOfType<WrongNumberOfDimensionInIndexMessage>());
		}
	}

}
