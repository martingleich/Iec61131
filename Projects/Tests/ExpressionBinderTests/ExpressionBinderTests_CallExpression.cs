using Compiler;
using Compiler.Messages;
using Compiler.Types;
using Xunit;

namespace Tests.ExpressionBinderTests
{
	using static ErrorTestHelper;

	public static class ExpressionBinderTests_CallExpression
	{
		public readonly static SystemScope SystemScope = BindHelper.SystemScope;

		[Fact]
		public static void NoArgFunctionWithReturn()
		{
			var boundExpression = BindHelper.NewProject
				.AddPou("FUNCTION MyFunc : INT", "MyFunc := 0;")
				.BindGlobalExpression<CallBoundExpression>("MyFunc()", null);
			Assert.Empty(boundExpression.Arguments);
			AssertEx.EqualType(SystemScope.Int, boundExpression.Type);
		}
		[Fact]
		public static void NoArgFunctionWithoutReturn()
		{
			var boundExpression = BindHelper.NewProject
				.AddPou("FUNCTION MyFunc", "")
				.BindGlobalExpression<CallBoundExpression>("MyFunc()", null);
			Assert.Empty(boundExpression.Arguments);
			AssertEx.EqualType(NullType.Instance, boundExpression.Type);
		}
		[Fact]
		public static void CastFunctionCallReturn()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION MyFunc : INT", "")
				.BindGlobalExpression<ImplicitCastBoundExpression>("MyFunc()", "DINT");
		}
		[Fact]
		public static void Error_WrongNumberOfArgs_NoReturn()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION MyFunc VAR_INPUT arg : INT; END_VAR", "")
				.BindGlobalExpression("MyFunc()", null, ErrorOfType<WrongNumberOfArgumentsMessage>());
		}
		[Fact]
		public static void Error_WrongNumberOfArgs_WithReturn()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION MyFunc : INT VAR_INPUT arg : INT; END_VAR", "")
				.BindGlobalExpression("MyFunc()", null, ErrorOfType<WrongNumberOfArgumentsMessage>());
		}
		[Fact]
		public static void Single_Arg_Implicit()
		{
			var boundExpression = BindHelper.NewProject
				.AddPou("FUNCTION MyFunc VAR_INPUT arg : INT; END_VAR", "")
				.BindGlobalExpression<CallBoundExpression>("MyFunc(0)", null);
			Assert.Collection(boundExpression.Arguments,
				arg => { Assert.Equal("arg".ToCaseInsensitive(), arg.ParameterSymbol.Name); });
		}
		[Fact]
		public static void Single_Arg_Implicit_Casted()
		{
			var boundExpression = BindHelper.NewProject
				.AddPou("FUNCTION MyFunc VAR_INPUT arg : DINT; END_VAR", "")
				.BindGlobalExpression<CallBoundExpression>("MyFunc(INT#0)", null);
			Assert.Collection(boundExpression.Arguments,
				arg =>
				{
					Assert.Equal("arg".ToCaseInsensitive(), arg.ParameterSymbol.Name);
					Assert.IsType<ImplicitCastBoundExpression>(arg.Value);
				});
		}
		[Fact]
		public static void Two_Arg_Implicit()
		{
			var boundExpression = BindHelper.NewProject
				.AddPou("FUNCTION MyFunc VAR_INPUT arg : INT; arg2 : BOOL; END_VAR", "")
				.BindGlobalExpression<CallBoundExpression>("MyFunc(0, FALSE)", null);
			Assert.Collection(boundExpression.Arguments,
				arg => { Assert.Equal("arg".ToCaseInsensitive(), arg.ParameterSymbol.Name); },
				arg => { Assert.Equal("arg2".ToCaseInsensitive(), arg.ParameterSymbol.Name); });
		}
		[Fact]
		public static void Error_ToManyArgs_Implicit()
		{
			var boundExpression = BindHelper.NewProject
				.AddPou("FUNCTION MyFunc VAR_INPUT arg : INT; END_VAR", "")
				.BindGlobalExpression<CallBoundExpression>("MyFunc(0, FALSE)", null, ErrorOfType<WrongNumberOfArgumentsMessage>());
			Assert.Collection(boundExpression.Arguments,
				arg => { Assert.Equal("arg".ToCaseInsensitive(), arg.ParameterSymbol.Name); },
				arg => { });
		}
		[Fact]
		public static void Error_Implicit_Output()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION MyFunc VAR_OUTPUT arg : INT; END_VAR", "")
				.WithGlobalVar("x", "INT")
				.BindGlobalExpression("MyFunc(x)", null, ErrorOfType<NonInputParameterMustBePassedExplicit>());
		}
		[Fact]
		public static void Error_Implicit_InOut()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION MyFunc VAR_IN_OUT arg : INT; END_VAR", "")
				.WithGlobalVar("x", "INT")
				.BindGlobalExpression("MyFunc(x)", null, ErrorOfType<NonInputParameterMustBePassedExplicit>());
		}

		[Theory]
		[InlineData("VAR_INPUT", ":=")]
		[InlineData("VAR_OUTPUT", "=>")]
		[InlineData("VAR_IN_OUT", ":=")]
		public static void Explicit_Arg(string decl, string op)
		{
			var boundExpression = BindHelper.NewProject
				.AddPou($"FUNCTION MyFunc {decl} arg : INT; END_VAR", "")
				.WithGlobalVar("x", "INT")
				.BindGlobalExpression<CallBoundExpression>($"MyFunc(arg {op} x)", null);
			Assert.Collection(boundExpression.Arguments,
				arg => { Assert.Equal("arg".ToCaseInsensitive(), arg.ParameterSymbol.Name); });
		}
		[Theory]
		[InlineData("VAR_INPUT", "=>")]
		[InlineData("VAR_OUTPUT", ":=")]
		[InlineData("VAR_IN_OUT", "=>")]
		public static void Error_Explicit_Arg_Mismatch(string decl, string op)
		{
			BindHelper.NewProject
				.AddPou($"FUNCTION MyFunc {decl} arg : INT; END_VAR", "")
				.WithGlobalVar("x", "INT")
				.BindGlobalExpression($"MyFunc(arg {op} x)", null, ErrorOfType<ParameterKindDoesNotMatchAssignMessage>());
		}

		[Fact]
		public static void Output_TypeCast()
		{
			var boundExpression = BindHelper.NewProject
				.AddPou($"FUNCTION MyFunc VAR_OUTPUT arg : INT; END_VAR", "")
				.WithGlobalVar("x", "DINT")
				.BindGlobalExpression<CallBoundExpression>($"MyFunc(arg => x)", null);
			Assert.Collection(boundExpression.Arguments,
				arg =>
				{
					Assert.IsType<ImplicitCastBoundExpression>(arg.Parameter);
					Assert.IsType<VariableBoundExpression>(arg.Value);
				});
		}

		[Fact]
		public static void Error_Output_Failed_TypeCast()
		{
			BindHelper.NewProject
				.AddPou($"FUNCTION MyFunc VAR_OUTPUT arg : BOOL; END_VAR", "")
				.WithGlobalVar("x", "DINT")
				.BindGlobalExpression<CallBoundExpression>($"MyFunc(arg => x)", null, ErrorOfType<TypeIsNotConvertibleMessage>());
		}

		[Fact]
		public static void Error_Output_NotWritable()
		{
			BindHelper.NewProject
				.AddPou($"FUNCTION MyFunc VAR_OUTPUT arg : INT; END_VAR", "")
				.BindGlobalExpression<CallBoundExpression>($"MyFunc(arg => 5)", null, ErrorOfType<CannotAssignToSyntaxMessage>());
		}

		[Fact]
		public static void Error_InOut_NotWritable()
		{
			BindHelper.NewProject
				.AddPou($"FUNCTION MyFunc VAR_IN_OUT arg : INT; END_VAR", "")
				.BindGlobalExpression<CallBoundExpression>($"MyFunc(arg := 5)", null, ErrorOfType<CannotAssignToSyntaxMessage>());
		}

		[Fact]
		public static void Error_InOut_NotConvertible()
		{
			BindHelper.NewProject
				.AddPou($"FUNCTION MyFunc VAR_IN_OUT arg : INT; END_VAR", "")
				.WithGlobalVar("x", "DINT")
				.BindGlobalExpression<CallBoundExpression>($"MyFunc(arg := x)", null, ErrorOfType<InoutArgumentMustHaveSameTypeMessage>());
		}

		[Fact]
		public static void Error_CannotCallSyntax()
		{
			BindHelper.NewProject
				.BindGlobalExpression($"7()", null, ErrorOfType<CannotCallTypeMessage>());
		}

		[Fact]
		public static void Error_ParameterWasAlreadyPassed()
		{
			BindHelper.NewProject
				.AddPou($"FUNCTION MyFunc VAR_INPUT arg : INT; arg2 : INT; END_VAR", "")
				.WithGlobalVar("x", "INT")
				.BindGlobalExpression<CallBoundExpression>($"MyFunc(arg := x, arg := 6)", null, ErrorOfType<ParameterWasAlreadyPassedMessage>());
		}

		[Fact]
		public static void Error_MissingArgument()
		{
			BindHelper.NewProject
				.AddPou($"FUNCTION MyFunc VAR_INPUT arg : INT; arg2 : INT; END_VAR", "")
				.WithGlobalVar("x", "INT")
				.BindGlobalExpression<CallBoundExpression>($"MyFunc(arg := x)", null, ErrorOfType<WrongNumberOfArgumentsMessage>());
		}

		[Fact]
		public static void Error_PositionalArgumentAfterExplicit()
		{
			BindHelper.NewProject
				.AddPou($"FUNCTION MyFunc VAR_INPUT arg : INT; arg2 : INT; arg3 : INT; END_VAR", "")
				.WithGlobalVar("x", "INT")
				.BindGlobalExpression<CallBoundExpression>($"MyFunc(x, arg2 := 5, x)", null, ErrorOfType<CannotUsePositionalParameterAfterExplicitMessage>());
		}
		[Fact]
		public static void ExplicitArgumentDiffrentOrder()
		{
			var boundExpression = BindHelper.NewProject
				.AddPou($"FUNCTION MyFunc VAR_INPUT arg1 : INT; arg2 : INT; arg3 : INT; END_VAR", "")
				.BindGlobalExpression<CallBoundExpression>($"MyFunc(arg3 := 0, arg2 := 1, arg1 := 2)", null);
			Assert.Collection(boundExpression.Arguments,
				arg => { Assert.Equal("arg3".ToCaseInsensitive(), arg.ParameterSymbol.Name); },
				arg => { Assert.Equal("arg2".ToCaseInsensitive(), arg.ParameterSymbol.Name); },
				arg => { Assert.Equal("arg1".ToCaseInsensitive(), arg.ParameterSymbol.Name); });
		}
		[Fact]
		public static void Error_UnknownExplicitParameter()
		{
			BindHelper.NewProject
				.AddPou($"FUNCTION MyFunc VAR_INPUT arg1 : INT; END_VAR", "")
				.BindGlobalExpression<CallBoundExpression>($"MyFunc(unknownArg := 7)", null, ErrorOfType<ParameterNotFoundMessage>());
		}
		
		[Fact]
		public static void ExplicitReadReturnValue()
		{
			BindHelper.NewProject
				.AddPou($"FUNCTION MyFunc : INT VAR_INPUT x : INT; END_VAR", "")
				.WithGlobalVar("x", "INT")
				.BindGlobalExpression<CallBoundExpression>($"MyFunc(MyFunc => x)", null, ErrorOfType<ParameterNotFoundMessage>());
		}

		[Fact]
		public static void CallFb()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION_BLOCK MyFb VAR_INPUT arg : INT; END_VAR", "")
				.WithGlobalVar("fb", "MyFb")
				.BindGlobalExpression<CallBoundExpression>("fb(arg := 17)", null);
		}
		[Fact]
		public static void CallFb_Complex()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo : MyFb", "")
				.AddPou("FUNCTION_BLOCK MyFb VAR_INPUT arg : INT; END_VAR", "")
				.BindGlobalExpression<CallBoundExpression>("foo()(arg := 17)", null);
		}

		[Fact]
		public static void CallUnknownSymbol()
		{
			BindHelper.NewProject
				.BindGlobalExpression("unknown_symbol(arg := 17)", null, ErrorOfType<VariableNotFoundMessage>());
		}
	}

}
