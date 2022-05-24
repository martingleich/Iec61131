using Compiler;
using Compiler.Messages;
using Xunit;

namespace CompilerTests.ExpressionBinderTests
{
	using static ErrorHelper;

	public static class ExpressionBinderTests_PointerArithmetic
	{
		[Fact]
		public static void PointerPlusInteger()
		{
			var (boundExpression, boundItf) = BindHelper.NewProject
				.WithGlobalVar("ptr", "POINTER TO REAL")
				.BindGlobalExpressionEx<PointerOffsetBoundExpression>("ptr + INT#5", null);
			AssertEx.NotAConstant(boundExpression, boundItf.SystemScope);
		}
		[Fact]
		public static void IntegerAddPointer()
		{
			var (boundExpression, boundItf) = BindHelper.NewProject
				.WithGlobalVar("ptr", "POINTER TO BOOL")
				.BindGlobalExpressionEx<PointerOffsetBoundExpression>("DINT#7 + ptr", null);
			AssertEx.NotAConstant(boundExpression, boundItf.SystemScope);
		}
		[Fact]
		public static void PointerSubInteger()
		{
			var (boundExpression, boundItf) = BindHelper.NewProject
				.WithGlobalVar("ptr", "POINTER TO BOOL")
				.BindGlobalExpressionEx<PointerOffsetBoundExpression>("ptr - SINT#7", null);
			AssertEx.NotAConstant(boundExpression, boundItf.SystemScope);
		}
		[Fact]
		public static void PointerSubPointer()
		{
			var (boundExpression, boundItf) = BindHelper.NewProject
				.WithGlobalVar("ptr", "POINTER TO BOOL")
				.WithGlobalVar("ptr2", "POINTER TO INT")
				.BindGlobalExpressionEx<PointerDiffrenceBoundExpression>("ptr2 - ptr", null);
			AssertEx.NotAConstant(boundExpression, boundItf.SystemScope);
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
