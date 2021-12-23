using Compiler;
using Compiler.Messages;
using Xunit;

namespace Tests.ExpressionBinderTests
{
	using static ErrorHelper;

	public class ExpressionBinder_IsLValue
	{
		[Fact]
		public static void Error_FunctionNotAssignable()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo", "")
				.AddPou("FUNCTION tester", "foo := foo;")
				.BindBodies(ErrorOfType<CannotAssignToVariableMessage>());
		}

		[Fact]
		public static void Error_InputVariableNotAssignable()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo VAR_INPUT x : INT; END_VAR", "x := 5;")
				.BindBodies(ErrorOfType<CannotAssignToVariableMessage>());
		}
		[Fact]
		public static void OutVariableAssignable()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo VAR_OUTPUT x : INT; END_VAR", "x := 5;")
				.BindBodies();
		}
		[Fact]
		public static void Error_FunctionCallNotAssignable()
		{
			var boundExpression = BindHelper.NewProject
				.AddPou("FUNCTION bar : INT", "bar := 0;")
				.BindGlobalExpression("bar()", null);
			Assert.True(IsLValueChecker.IsLValue(boundExpression).HasErrors);
		}
		[Fact]
		public static void Error_LiteralNotAssignable()
		{
			var boundExpression = BindHelper.NewProject
				.BindGlobalExpression("7", null);
			Assert.True(IsLValueChecker.IsLValue(boundExpression).HasErrors);
		}
		[Fact]
		public static void Error_UnaryOpNotAssignable()
		{
			var boundExpression = BindHelper.NewProject
				.WithGlobalVar("x", "INT")
				.BindGlobalExpression("-x", null);
			Assert.True(IsLValueChecker.IsLValue(boundExpression).HasErrors);
		}
		[Fact]
		public static void Error_BinaryOpNotAssignable()
		{
			var boundExpression = BindHelper.NewProject
				.BindGlobalExpression("(1-6)", null);
			Assert.True(IsLValueChecker.IsLValue(boundExpression).HasErrors);
		}
		[Fact]
		public static void Error_SizeofAssignable()
		{
			var boundExpression = BindHelper.NewProject
				.BindGlobalExpression("SIZEOF(INT)", null);
			Assert.True(IsLValueChecker.IsLValue(boundExpression).HasErrors);
		}
		[Fact]
		public static void Error_PointerOffsetAssignable()
		{
			var boundExpression = BindHelper.NewProject
				.WithGlobalVar("ptr", "POINTER TO INT")
				.BindGlobalExpression("ptr - 3", null);
			Assert.True(IsLValueChecker.IsLValue(boundExpression).HasErrors);
		}
		[Fact]
		public static void Error_PointerDiffrenceAssignable()
		{
			var boundExpression = BindHelper.NewProject
				.WithGlobalVar("ptr", "POINTER TO INT")
				.WithGlobalVar("ptr2", "POINTER TO INT")
				.BindGlobalExpression("ptr - ptr2", null);
			Assert.True(IsLValueChecker.IsLValue(boundExpression).HasErrors);
		}
		[Fact]
		public static void Error_InitializerAssignable()
		{
			var boundExpression = BindHelper.NewProject
				.BindGlobalExpression("{1}", "ARRAY[0..0] OF INT");
			Assert.True(IsLValueChecker.IsLValue(boundExpression).HasErrors);
		}
		[Fact]
		public static void Error_PointerCaseAssignable()
		{
			var boundExpression = BindHelper.NewProject
				.WithGlobalVar("ptr", "POINTER TO INT")
				.BindGlobalExpression("ptr", "POINTER TO REAL");
			Assert.True(IsLValueChecker.IsLValue(boundExpression).HasErrors);
		}
	}
}
