using Compiler;
using Compiler.Messages;
using Xunit;

namespace Tests.ExpressionBinderTests
{
	using static ErrorHelper;

	public static class ExpressionBinderTests_PointerArithmetic
	{
		private static readonly SystemScope SystemScope = BindHelper.SystemScope;

		[Fact]
		public static void PointerPlusInteger()
		{
			var boundExpression = BindHelper.NewProject
				.WithGlobalVar("ptr", "POINTER TO REAL")
				.BindGlobalExpression("ptr + INT#5", null);
			Assert.IsType<PointerOffsetBoundExpression>(boundExpression);
			AssertEx.NotAConstant(boundExpression, SystemScope);
		}
		[Fact]
		public static void IntegerAddPointer()
		{
			var boundExpression = BindHelper.NewProject
				.WithGlobalVar("ptr", "POINTER TO BOOL")
				.BindGlobalExpression("DINT#7 + ptr", null);
			Assert.IsType<PointerOffsetBoundExpression>(boundExpression);
			AssertEx.NotAConstant(boundExpression, SystemScope);
		}
		[Fact]
		public static void PointerSubInteger()
		{
			var boundExpression = BindHelper.NewProject
				.WithGlobalVar("ptr", "POINTER TO BOOL")
				.BindGlobalExpression("ptr - SINT#7", null);
			Assert.IsType<PointerOffsetBoundExpression>(boundExpression);
			AssertEx.NotAConstant(boundExpression, SystemScope);
		}
		[Fact]
		public static void PointerSubPointer()
		{
			var boundExpression = BindHelper.NewProject
				.WithGlobalVar("ptr", "POINTER TO BOOL")
				.WithGlobalVar("ptr2", "POINTER TO INT")
				.BindGlobalExpression("ptr2 - ptr", null);
			Assert.IsType<PointerDiffrenceBoundExpression>(boundExpression);
			AssertEx.NotAConstant(boundExpression, SystemScope);
		}
		[Fact]
		public static void Error_PointerAddPointer()
		{
			BindHelper.NewProject
				.WithGlobalVar("ptr", "POINTER TO BOOL")
				.WithGlobalVar("ptr2", "POINTER TO INT")
				.BindGlobalExpression("ptr2 + ptr", null, ErrorOfType<CannotPerformArithmeticOnTypesMessage>());
		}
	}

}
