using Compiler.Messages;
using Xunit;

namespace Tests.ExpressionBinderTests
{
	using static ErrorTestHelper;

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
			BindHelper.NewProject
				.AddPou("FUNCTION bar : INT", "bar := 0;")
				.AddPou("FUNCTION foo", "bar() := 5;")
				.BindBodies(ErrorOfType<CannotAssignToSyntaxMessage>());
		}
		[Fact]
		public static void Error_LiteralNotAssignable()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo", "6 := 5;")
				.BindBodies(ErrorOfType<CannotAssignToSyntaxMessage>());
		}
		[Fact]
		public static void Error_UnaryOpNotAssignable()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo VAR_IN_OUT x : INT; END_VAR", "(-x) := 5;")
				.BindBodies(ErrorOfType<CannotAssignToSyntaxMessage>());
		}
		[Fact]
		public static void Error_BinaryOpNotAssignable()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo", "(1-6) := 5;")
				.BindBodies(ErrorOfType<CannotAssignToSyntaxMessage>());
		}
		[Fact]
		public static void Error_SizeofAssignable()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo", "SIZEOF(INT) := 5;")
				.BindBodies(ErrorOfType<CannotAssignToSyntaxMessage>());
		}
		[Fact]
		public static void Error_PointerOffsetAssignable()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo VAR_IN_OUT ptr : POINTER TO INT; END_VAR ", "(ptr - 3) := 0;")
				.BindBodies(ErrorOfType<CannotAssignToSyntaxMessage>());
		}
		[Fact]
		public static void Error_PointerDiffrenceAssignable()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo VAR_IN_OUT ptr : POINTER TO INT; ptr2 : POINTER TO INT; END_VAR ", "(ptr - ptr2) := 5;")
				.BindBodies(ErrorOfType<CannotAssignToSyntaxMessage>());
		}
	}
}
