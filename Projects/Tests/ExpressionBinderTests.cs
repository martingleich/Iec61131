using Compiler;
using Compiler.Messages;
using Compiler.Types;
using Xunit;

namespace Tests
{
	using static ErrorTestHelper;
	public static class ExpressionBinderTests
	{
		private static readonly SystemScope SystemScope = BindHelper.SystemScope;
		public static class Literal
		{
			public static readonly object[][] NoTargetTypeValues = {
				new object[]{"1", SystemScope.Int },
				new object[]{ushort.MaxValue.ToString(), SystemScope.UInt },
				new object[]{int.MaxValue.ToString(), SystemScope.DInt },
				new object[]{uint.MaxValue.ToString(), SystemScope.UDInt },
				new object[]{long.MaxValue.ToString(), SystemScope.LInt },
				new object[]{ulong.MaxValue.ToString(), SystemScope.ULInt },
				new object[]{int.MinValue.ToString(), SystemScope.DInt },
				new object[]{long.MinValue.ToString(), SystemScope.LInt },
				new object[]{"TRUE", SystemScope.Bool },
				new object[]{"FALSE", SystemScope.Bool },
				new object[]{"1.5", SystemScope.LReal },
				// Typed boolean
				new object[]{"BOOL#FALSE", SystemScope.Bool },
				new object[]{"BOOL#TRUE", SystemScope.Bool },
				new object[]{"BOOL#0", SystemScope.Bool },
				new object[]{"BOOL#1", SystemScope.Bool },
				// Typed integer
				new object[]{"SINT#1", SystemScope.SInt },
				new object[]{"USINT#1", SystemScope.USInt },
				new object[]{"INT#1", SystemScope.Int },
				new object[]{"UINT#1", SystemScope.UInt },
				new object[]{"DINT#1", SystemScope.DInt },
				new object[]{"UDINT#1", SystemScope.UDInt },
				new object[]{"LINT#1", SystemScope.LInt },
				new object[]{"ULINT#1", SystemScope.ULInt },
				// Typed real
				new object[]{"REAL#1", SystemScope.Real },
				new object[]{"REAL#1.5", SystemScope.Real },
				new object[]{"LREAL#1", SystemScope.LReal },
				new object[]{"LREAL#1.5", SystemScope.LReal },
			};
			public static readonly object[][] TargetType_DoesNotFit_Values = {
				new object[]{"BOOL#7" },
				new object[]{$"SINT#{byte.MaxValue}"},
				new object[]{"USINT#-1"},
				new object[]{$"INT#{ushort.MaxValue}"},
				new object[]{"UINT#-1"},
				new object[]{$"DINT#{uint.MaxValue}"},
				new object[]{"UDINT#-1"},
				new object[]{$"LINT#{ulong.MaxValue}"},
				new object[]{"ULINT#-1"},

			};
			[Theory]
			[MemberData(nameof(NoTargetTypeValues))]
			public static void NoTargetType(string value, IType resultType)
			{
				var boundExpression = BindHelper.NewProject
					.BindGlobalExpression(value, null);
				var literalBound = Assert.IsType<LiteralBoundExpression>(boundExpression);
				Assert.Equal(literalBound.Type, resultType);
			}
			[Theory]
			[MemberData(nameof(TargetType_DoesNotFit_Values))]
			public static void TargetType_DoesNotFit(string value)
			{
				BindHelper.NewProject
					.BindGlobalExpression(value, null, ErrorOfType<IntegerIsToLargeForTypeMessage>());
			}

			[Fact]
			public static void Int_NoTargetType_ToBig()
			{
				BindHelper.NewProject
					.BindGlobalExpression("99999999999999999999999999999999999999", null, ErrorOfType<ConstantDoesNotFitIntoAnyType>());
			}
			[Fact]
			public static void LReal_NoTargetType_ToBig()
			{
				BindHelper.NewProject
					.BindGlobalExpression(new string('9', 500) + ".0", null, ErrorOfType<RealIsToLargeForTypeMessage>());
			}

			[Fact]
			public static void Pointer_ZeroAsPointer()
			{
				var boundExpression = BindHelper.NewProject
					.BindGlobalExpression("0", "POINTER TO INT");
				AssertEx.EqualType(new PointerType(SystemScope.Int), boundExpression.Type);
			}
			[Fact]
			public static void Error_Pointer_OneAsPointer()
			{
				BindHelper.NewProject
					.BindGlobalExpression("1", "POINTER TO INT", ErrorOfType<IntegerIsToLargeForTypeMessage>());
			}
		}

		[Fact]
		public static void ConvertPointerToPointer()
		{
			var boundExpression = BindHelper.NewProject
				.WithGlobalVar("ptr", "POINTER TO INT")
				.BindGlobalExpression("ptr", "POINTER TO REAL");
			var pointerCast = Assert.IsType<ImplicitPointerTypeCastBoundExpression>(boundExpression);
			AssertEx.EqualType(pointerCast.Type, new PointerType(SystemScope.Real));
		}

		[Theory]
		[InlineData("SINT#0 + SINT#0", "ADD_SINT")]
		[InlineData("SINT#0 - SINT#0", "SUB_SINT")]
		[InlineData("SINT#0 * SINT#0", "MUL_SINT")]
		[InlineData("SINT#0 / SINT#0", "DIV_SINT")]
		[InlineData("SINT#0 + LINT#0", "ADD_LINT")]
		[InlineData("INT#0 + SINT#0", "ADD_INT")]
		[InlineData("INT#0 - INT#0", "SUB_INT")]
		[InlineData("INT#0 * INT#0", "MUL_INT")]
		[InlineData("INT#0 / INT#0", "DIV_INT")]
		[InlineData("DINT#0 + SINT#0", "ADD_DINT")]
		[InlineData("DINT#0 - INT#0", "SUB_DINT")]
		[InlineData("DINT#0 * DINT#0", "MUL_DINT")]
		[InlineData("DINT#0 / DINT#0", "DIV_DINT")]
		[InlineData("LINT#0 + SINT#0", "ADD_LINT")]
		[InlineData("LINT#0 - INT#0", "SUB_LINT")]
		[InlineData("LINT#0 * DINT#0", "MUL_LINT")]
		[InlineData("LINT#0 / LINT#0", "DIV_LINT")]

		[InlineData("USINT#0 + USINT#0", "ADD_USINT")]
		[InlineData("USINT#0 - USINT#0", "SUB_USINT")]
		[InlineData("USINT#0 * USINT#0", "MUL_USINT")]
		[InlineData("USINT#0 / USINT#0", "DIV_USINT")]
		[InlineData("UINT#0 + USINT#0", "ADD_UINT")]
		[InlineData("UINT#0 - UINT#0", "SUB_UINT")]
		[InlineData("UINT#0 * UINT#0", "MUL_UINT")]
		[InlineData("UINT#0 / UINT#0", "DIV_UINT")]
		[InlineData("UDINT#0 + USINT#0", "ADD_UDINT")]
		[InlineData("UDINT#0 - UINT#0", "SUB_UDINT")]
		[InlineData("UDINT#0 * UDINT#0", "MUL_UDINT")]
		[InlineData("UDINT#0 / UDINT#0", "DIV_UDINT")]
		[InlineData("ULINT#0 + USINT#0", "ADD_ULINT")]
		[InlineData("ULINT#0 - UINT#0", "SUB_ULINT")]
		[InlineData("ULINT#0 * UDINT#0", "MUL_ULINT")]
		[InlineData("ULINT#0 / ULINT#0", "DIV_ULINT")]

		[InlineData("UINT#0 + SINT#0", "ADD_DINT")]
		[InlineData("USINT#0 + SINT#0", "ADD_INT")]
		[InlineData("DINT#0 + UINT#0", "ADD_LINT")]

		[InlineData("REAL#0 + LREAL#0", "ADD_LREAL")]
		[InlineData("REAL#0 + INT#0", "ADD_REAL")]
		[InlineData("INT#0 + LREAL#0", "ADD_LREAL")]
		[InlineData("LREAL#0 + SINT#0", "ADD_LREAL")]
		[InlineData("UINT#0 + REAL#0", "ADD_REAL")]
		public static void BinaryArithemtic(string expr, string op)
		{
			var boundExpression = BindHelper.NewProject
				.BindGlobalExpression(expr, null);
			var binaryExpression = Assert.IsType<BinaryOperatorBoundExpression>(boundExpression);
			Assert.Equal(op.ToCaseInsensitive(), binaryExpression.Function.Name);
		}
		[Theory]
		[InlineData("INT#0 = INT#1", "EQUAL_INT", false)]
		[InlineData("INT#0 <> INT#1", "NOT_EQUAL_INT", true)]
		[InlineData("INT#0 < INT#1", "LESS_INT", true)]
		[InlineData("INT#2 <= INT#2", "LESS_EQUAL_INT", true)]
		[InlineData("INT#1 >= INT#5", "GREATER_EQUAL_INT", false)]
		[InlineData("INT#7 > INT#5", "GREATER_INT", true)]
		[InlineData("SINT#3 > LINT#5", "GREATER_LINT", false)]
		[InlineData("USINT#5 > INT#5", "GREATER_DINT", false)]
		[InlineData("BOOL#TRUE = BOOL#FALSE", "EQUAL_BOOL", false)]
		[InlineData("BOOL#FALSE <> BOOL#TRUE", "NOT_EQUAL_BOOL", true)]
		public static void Comparisions(string expr, string op, bool result)
		{
			var boundExpression = BindHelper.NewProject
				.BindGlobalExpression(expr, null);
			var binaryExpression = Assert.IsType<BinaryOperatorBoundExpression>(boundExpression);
			Assert.Equal(op.ToCaseInsensitive(), binaryExpression.Function.Name);
			AssertEx.EqualType(SystemScope.Bool, binaryExpression.Type);
			var bag = new MessageBag();
			var actualResult = ConstantExpressionEvaluator.EvaluateConstant(SystemScope, boundExpression, bag);
			Assert.Empty(bag);
			Assert.Equal(result, Assert.IsType<BooleanLiteralValue>(actualResult).Value);
		}
		[Fact]
		public static void Error_ConstantDivisionByZero()
		{
			var boundExpression = BindHelper.NewProject
				.BindGlobalExpression("DINT#77 / DINT#0", null);
			var bag = new MessageBag();
			var actualResult = ConstantExpressionEvaluator.EvaluateConstant(SystemScope, boundExpression, bag);
			ExactlyMessages(ErrorOfType<DivsionByZeroInConstantContextMessage>())(bag);
			Assert.Null(actualResult);
		}
		[Fact]
		public static void Error_Constant_Overflow()
		{
			var boundExpression = BindHelper.NewProject
				.BindGlobalExpression("USINT#200 + USINT#200", null);
			var bag = new MessageBag();
			var actualResult = ConstantExpressionEvaluator.EvaluateConstant(SystemScope, boundExpression, bag);
			ExactlyMessages(ErrorOfType<OverflowInConstantContextMessage>())(bag);
			Assert.Null(actualResult);
		}
		[Fact]
		public static void Error_Constant_NonConstantOperator_LREAL()
		{
			var boundExpression = BindHelper.NewProject
				.BindGlobalExpression("LREAL#1 + LREAL#2", null);
			var bag = new MessageBag();
			var actualResult = ConstantExpressionEvaluator.EvaluateConstant(SystemScope, boundExpression, bag);
			ExactlyMessages(ErrorOfType<NotAConstantMessage>())(bag);
			Assert.Null(actualResult);
		}
		[Fact]
		public static void Error_Constant_NonConstantOperator_LREAL_NoCascading_Binary()
		{
			var boundExpression = BindHelper.NewProject
				.BindGlobalExpression("LREAL#0 + (REAL#1 + REAL#2)", null);
			var bag = new MessageBag();
			var actualResult = ConstantExpressionEvaluator.EvaluateConstant(SystemScope, boundExpression, bag);
			ExactlyMessages(ErrorOfType<NotAConstantMessage>())(bag);
			Assert.Null(actualResult);
		}
		[Fact]
		public static void Error_Constant_NonConstantOperator_LREAL_NoCascading_Unary()
		{
			var boundExpression = BindHelper.NewProject
				.BindGlobalExpression("NOT (LREAL#1 <> LREAL#2)", null);
			var bag = new MessageBag();
			var actualResult = ConstantExpressionEvaluator.EvaluateConstant(SystemScope, boundExpression, bag);
			ExactlyMessages(ErrorOfType<NotAConstantMessage>())(bag);
			Assert.Null(actualResult);
		}

		[Theory]
		[InlineData("NOT BOOL#FALSE", "NOT_BOOL", true)]
		[InlineData("NOT BOOL#TRUE", "NOT_BOOL", false)]
		public static void UnaryExpression_Boolean(string expr, string op, bool result)
		{
			var boundExpression = BindHelper.NewProject
				.BindGlobalExpression(expr, null);
			var unaryExpression = Assert.IsType<UnaryOperatorBoundExpression>(boundExpression);
			Assert.Equal(op.ToCaseInsensitive(), unaryExpression.Function.Name);
			var bag = new MessageBag();
			var actualResult = ConstantExpressionEvaluator.EvaluateConstant(SystemScope, boundExpression, bag);
			Assert.Empty(bag);
			Assert.Equal(result, Assert.IsType<BooleanLiteralValue>(actualResult).Value);
		}
		[Theory]
		[InlineData("-(INT#7)", "NEG_INT")]
		[InlineData("-(SINT#7)", "NEG_SINT")]
		[InlineData("-(DINT#7)", "NEG_DINT")]
		[InlineData("-(LINT#7)", "NEG_LINT")]
		[InlineData("-(REAL#7)", "NEG_REAL")]
		[InlineData("-(LREAL#7)", "NEG_LREAL")]
		public static void UnaryExpression(string expr, string op)
		{
			var boundExpression = BindHelper.NewProject
				.BindGlobalExpression(expr, null);
			var unaryExpression = Assert.IsType<UnaryOperatorBoundExpression>(boundExpression);
			Assert.Equal(op.ToCaseInsensitive(), unaryExpression.Function.Name);
		}
		[Theory]
		[InlineData("-(UINT#7)")]
		[InlineData("-(ULINT#7)")]
		[InlineData("-(BOOL#FALSE)")]
		public static void Error_UnaryExpression_NoNegAllowed(string expr)
		{
			BindHelper.NewProject
				.BindGlobalExpression(expr, null, ErrorOfType<CannotPerformArithmeticOnTypesMessage>());
		}
		[Fact]
		public static void Error_NegateOnDUT()
		{
			BindHelper.NewProject
				.AddDut("TYPE MyDut : STRUCT END_STRUCT; END_TYPE")
				.WithGlobalVar("x", "MyDut")
				.BindGlobalExpression("-x", null, ErrorOfType<CannotPerformArithmeticOnTypesMessage>());
		}

		[Theory]
		[InlineData("enumValue + 1", "ADD_INT")]
		[InlineData("LREAL#5 + enumValue", "ADD_LREAL")]
		public static void BinaryArithemtic_Enums(string expr, string op)
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("TYPE MyEnum : (First, Second); END_TYPE")
				.WithGlobalVar("enumValue", "MyEnum")
				.BindGlobalExpression(expr, null);
			var binaryExpression = Assert.IsType<BinaryOperatorBoundExpression>(boundExpression);
			Assert.Equal(op.ToCaseInsensitive(), binaryExpression.Function.Name);
		}

		[Fact]
		public static void Error_BinaryArithemtic_UnsupportedTypes_ToLarge_LINT_USINT()
		{
			BindHelper.NewProject
				.BindGlobalExpression("LINT#0 + USINT#0", null, ErrorOfType<CannotPerformArithmeticOnTypesMessage>());
		}
		[Fact]
		public static void Error_BinaryArithemtic_UnsupportedTypes_NoArithmetic_LINT_BOOL()
		{
			BindHelper.NewProject
				.BindGlobalExpression("LINT#0 + TRUE", null, ErrorOfType<CannotPerformArithmeticOnTypesMessage>());
		}
		[Fact]
		public static void Error_BinaryArithemtic_UnsupportedTypes_NoArithmetic_BOOL_DINT()
		{
			BindHelper.NewProject
				.BindGlobalExpression("FALSE + DINT#0", null, ErrorOfType<CannotPerformArithmeticOnTypesMessage>());
		}
		[Fact]
		public static void Error_BinaryArithemtic_UnsupportedTypes_NoArithmetic_Dut()
		{
			BindHelper.NewProject
				.AddDut("TYPE MyType : STRUCT END_STRUCT; END_TYPE")
				.WithGlobalVar("dutVar", "MyType")
				.BindGlobalExpression("INT#0 + dutVar", null, ErrorOfType<CannotPerformArithmeticOnTypesMessage>());
		}

		[Fact]
		public static void Error_TypeNotConvertible_INT_TO_BOOL()
		{
			BindHelper.NewProject
				.BindGlobalExpression("TRUE", "INT", ErrorOfType<TypeIsNotConvertibleMessage>());
		}
		[Fact]
		public static void ParenthesisedExpression()
		{
			var boundExpression = BindHelper.NewProject
				.BindGlobalExpression("(TRUE)", null);
			var literalBound = Assert.IsType<LiteralBoundExpression>(boundExpression);
			AssertEx.EqualType(SystemScope.Bool, literalBound.Type);
		}
		[Fact]
		public static void ParenthesisedExpression_KeepContext()
		{
			var boundExpression = BindHelper.NewProject
				.BindGlobalExpression("(0)", "DINT");
			var literalBound = Assert.IsType<LiteralBoundExpression>(boundExpression);
			AssertEx.EqualType(SystemScope.DInt, literalBound.Type);
		}
		[Fact]
		public static void Deref()
		{
			var boundExpression = BindHelper.NewProject
				.WithGlobalVar("ptr", "POINTER TO BOOL")
				.BindGlobalExpression("ptr^", null);
			var deref = Assert.IsType<DerefBoundExpression>(boundExpression);
			AssertEx.EqualType(deref.Type, SystemScope.Bool);
		}
		[Fact]
		public static void Error_Deref_IntValue()
		{
			var boundExpression = BindHelper.NewProject
				.WithGlobalVar("intValue", "INT")
				.BindGlobalExpression("intValue^", null, ErrorOfType<CannotDereferenceTypeMessage>());
			var deref = Assert.IsType<DerefBoundExpression>(boundExpression);
			AssertEx.EqualType(deref.Type, SystemScope.Int);
		}
		[Fact]
		public static void Error_Deref_NotAConstant()
		{
			var boundExpression = BindHelper.NewProject
				.WithGlobalVar("ptr", "POINTER TO BOOL")
				.BindGlobalExpression("ptr^", null);
			AssertEx.NotAConstant(boundExpression, SystemScope);
		}
	}

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
			Assert.IsType<ImplicitArithmeticCastBoundExpression>(boundExpression);
			AssertEx.EqualType("INT", boundExpression.Type);
		}
		[Fact]
		public static void IndexAccessDut()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("TYPE myDut : STRUCT field : REAL; END_STRUCT; END_TYPE")
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
				.AddDut("TYPE myalias : ARRAY[0..10] OF INT; END_TYPE")
				.WithGlobalVar("arr", "myalias")
				.BindGlobalExpression("arr[0]", null);
		}
		[Fact]
		public static void IndexAccess_ToAliasOfPointer()
		{
			BindHelper.NewProject
				.AddDut("TYPE myalias : POINTER TO INT; END_TYPE")
				.WithGlobalVar("arr", "myalias")
				.BindGlobalExpression("arr[0]", null);
		}
		[Fact]
		public static void IndexAccess_WithAliasToInt()
		{
			BindHelper.NewProject
				.AddDut("TYPE myalias : INT; END_TYPE")
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

	public static class ExpressionBinderTests_CompoAccess
	{
		[Fact]
		public static void Error_NonStructuredType_Int()
		{
			BindHelper.NewProject
				.WithGlobalVar("value", "INT")
				.BindGlobalExpression("value.xyz", null, ErrorOfType<FieldNotFoundMessage>());
		}
		[Fact]
		public static void Error_StructuredType_DoesNotContainField()
		{
			BindHelper.NewProject
				.AddDut("TYPE myDut : STRUCT myField : USINT; END_STRUCT; END_TYPE")
				.WithGlobalVar("value", "myDut")
				.BindGlobalExpression("value.abc", null, ErrorOfType<FieldNotFoundMessage>());
		}
		[Fact]
		public static void Error_NoCascadingError()
		{
			BindHelper.NewProject
				.BindGlobalExpression("(TRUE * FALSE).abc", null, ErrorOfType<CannotPerformArithmeticOnTypesMessage>());
		}
		[Fact]
		public static void FieldOnVariable()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("TYPE myDut : STRUCT myField : USINT; END_STRUCT; END_TYPE")
				.WithGlobalVar("value", "myDut")
				.BindGlobalExpression("value.myField", null);
			var fieldAccess = Assert.IsType<FieldAccessBoundExpression>(boundExpression);
			Assert.Equal("myField".ToCaseInsensitive(), fieldAccess.Field.Name);
		}
		[Fact]
		public static void FieldOnIndex()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("TYPE myDut : STRUCT myField : USINT; END_STRUCT; END_TYPE")
				.WithGlobalVar("values", "ARRAY[0..10] OF myDut")
				.BindGlobalExpression("values[1].myField", null);
			var fieldAccess = Assert.IsType<FieldAccessBoundExpression>(boundExpression);
			Assert.Equal("myField".ToCaseInsensitive(), fieldAccess.Field.Name);
		}
		[Fact]
		public static void FieldCastedResult()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("TYPE myDut : STRUCT myField : USINT; END_STRUCT; END_TYPE")
				.WithGlobalVar("value", "myDut")
				.BindGlobalExpression("value.myField", "DINT");
			Assert.IsType<ImplicitArithmeticCastBoundExpression>(boundExpression);
		}

		[Fact]
		public static void Error_TypeNoStatic()
		{
			BindHelper.NewProject
				.AddDut("TYPE myDut : STRUCT myField : USINT; END_STRUCT; END_TYPE")
				.BindGlobalExpression("myDut.myField", null, ErrorOfType<TypeDoesNotContainStaticVariableMessage>());
		}
		[Fact]
		public static void EnumTypeValue()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("TYPE myEnum : (elem1, elem2); END_TYPE")
				.BindGlobalExpression("myEnum.elem2", null);
			var literalExpr = Assert.IsType<LiteralBoundExpression>(boundExpression);
			var value = Assert.IsType<EnumLiteralValue>(literalExpr.Value);
			var innerValue = Assert.IsType<IntLiteralValue>(value.InnerValue);
			Assert.Equal(1, innerValue.Value);
		}
		[Fact]
		public static void EnumTypeValue_ViaAlias()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("TYPE myEnum : (elem1, elem2); END_TYPE")
				.AddDut("TYPE myAlias : myEnum; END_TYPE")
				.BindGlobalExpression("myAlias.elem2", null);
			var castExpr = Assert.IsType<ImplicitAliasFromBaseTypeCastBoundExpression>(boundExpression);
			var literalExpr = Assert.IsType<LiteralBoundExpression>(castExpr.Value);
			var value = Assert.IsType<EnumLiteralValue>(literalExpr.Value);
			var innerValue = Assert.IsType<IntLiteralValue>(value.InnerValue);
			Assert.Equal(1, innerValue.Value);
		}
		[Fact]
		public static void Error_EnumTypeValue_Missing()
		{
			BindHelper.NewProject
				.AddDut("TYPE myEnum : (elem1, elem2); END_TYPE")
				.BindGlobalExpression("myEnum.elem3", null, ErrorOfType<EnumValueNotFoundMessage>());
		}
		[Fact]
		public static void GvlVariable()
		{
			var boundExpression = BindHelper.NewProject
				.AddGVL("MyGVL", "VAR_GLOBAL gVar : INT; END_VAR")
				.BindGlobalExpression("MyGVL.gVar", null);
			var staticVarExpr = Assert.IsType<StaticVariableBoundExpression>(boundExpression);
			Assert.Equal("gVar".ToCaseInsensitive(), staticVarExpr.Variable.Name);
		}
		[Fact]
		public static void GvlVariable_Cast()
		{
			var boundExpression = BindHelper.NewProject
				.AddGVL("MyGVL", "VAR_GLOBAL gVar : INT; END_VAR")
				.BindGlobalExpression("MyGVL.gVar", "DINT");
			Assert.IsType<ImplicitArithmeticCastBoundExpression>(boundExpression);
		}
		[Fact]
		public static void Error_GvlVariable_Missing()
		{
			BindHelper.NewProject
				.AddGVL("MyGVL", "VAR_GLOBAL gVar : INT; END_VAR")
				.BindGlobalExpression("MyGVL.abc", null, ErrorOfType<GlobalVariableNotFoundMessage>());
		}
		[Fact]
		public static void VariableBeforeGvl()
		{
			var boundExpression = BindHelper.NewProject
				.AddGVL("MyGVL", "VAR_GLOBAL myVar : INT; END_VAR")
				.AddDut("TYPE myDut : STRUCT myVar : INT; END_STRUCT; END_TYPE")
				.WithGlobalVar("MyGvl", "myDut")
				.BindGlobalExpression("MyGVL.myVar", null);
			Assert.IsType<FieldAccessBoundExpression>(boundExpression);
		}
		[Fact]
		public static void Error_UnknownLeftSide()
		{
			BindHelper.NewProject
				.BindGlobalExpression("myThing.myVar", null, ErrorOfType<ExpectedVariableOrTypeOrGvlMessage>());
		}
		[Fact]
		public static void DutInGvlAccess()
		{
			var boundExpression = BindHelper.NewProject
				.AddGVL("MyGVL", "VAR_GLOBAL myVar : MyDut; END_VAR")
				.AddDut("TYPE MyDut : STRUCT myField : INT; END_STRUCT; END_TYPE")
				.BindGlobalExpression("MyGVL.myVar.myField", null);
			var fieldAccess = Assert.IsType<FieldAccessBoundExpression>(boundExpression);
			Assert.IsType<StaticVariableBoundExpression>(fieldAccess.BaseExpression);
		}
	}

	public static class ExpressionBinderTests_CallExpression
	{
		public readonly static SystemScope SystemScope = BindHelper.SystemScope;

		[Fact]
		public static void NoArgFunctionWithReturn()
		{
			var boundExpression = BindHelper.NewProject
				.AddPou("FUNCTION MyFunc : INT", "MyFunc := 0;")
				.BindGlobalExpression<FunctionCallBoundExpression>("MyFunc()", null);
			Assert.Empty(boundExpression.Arguments);
			AssertEx.EqualType(SystemScope.Int, boundExpression.Type);
		}
		[Fact]
		public static void NoArgFunctionWithoutReturn()
		{
			var boundExpression = BindHelper.NewProject
				.AddPou("FUNCTION MyFunc", "")
				.BindGlobalExpression<FunctionCallBoundExpression>("MyFunc()", null);
			Assert.Empty(boundExpression.Arguments);
			AssertEx.EqualType(NullType.Instance, boundExpression.Type);
		}
		[Fact]
		public static void CastFunctionCallReturn()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION MyFunc : INT", "")
				.BindGlobalExpression<ImplicitArithmeticCastBoundExpression>("MyFunc()", "DINT");
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
				.BindGlobalExpression<FunctionCallBoundExpression>("MyFunc(0)", null);
			Assert.Collection(boundExpression.Arguments,
				arg => { Assert.Equal("arg".ToCaseInsensitive(), arg.ParameterSymbol.Name); });
		}
		[Fact]
		public static void Single_Arg_Implicit_Casted()
		{
			var boundExpression = BindHelper.NewProject
				.AddPou("FUNCTION MyFunc VAR_INPUT arg : DINT; END_VAR", "")
				.BindGlobalExpression<FunctionCallBoundExpression>("MyFunc(INT#0)", null);
			Assert.Collection(boundExpression.Arguments,
				arg =>
				{
					Assert.Equal("arg".ToCaseInsensitive(), arg.ParameterSymbol.Name);
					Assert.IsType<ImplicitArithmeticCastBoundExpression>(arg.Value);
				});
		}
		[Fact]
		public static void Two_Arg_Implicit()
		{
			var boundExpression = BindHelper.NewProject
				.AddPou("FUNCTION MyFunc VAR_INPUT arg : INT; arg2 : BOOL; END_VAR", "")
				.BindGlobalExpression<FunctionCallBoundExpression>("MyFunc(0, FALSE)", null);
			Assert.Collection(boundExpression.Arguments,
				arg => { Assert.Equal("arg".ToCaseInsensitive(), arg.ParameterSymbol.Name); },
				arg => { Assert.Equal("arg2".ToCaseInsensitive(), arg.ParameterSymbol.Name); });
		}
		[Fact]
		public static void Error_ToManyArgs_Implicit()
		{
			var boundExpression = BindHelper.NewProject
				.AddPou("FUNCTION MyFunc VAR_INPUT arg : INT; END_VAR", "")
				.BindGlobalExpression<FunctionCallBoundExpression>("MyFunc(0, FALSE)", null, ErrorOfType<WrongNumberOfArgumentsMessage>());
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
				.BindGlobalExpression<FunctionCallBoundExpression>($"MyFunc(arg {op} x)", null);
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
				.BindGlobalExpression<FunctionCallBoundExpression>($"MyFunc(arg => x)", null);
			Assert.Collection(boundExpression.Arguments,
				arg =>
				{
					Assert.IsType<ImplicitArithmeticCastBoundExpression>(arg.Parameter);
					Assert.IsType<VariableBoundExpression>(arg.Value);
				});
		}

		[Fact]
		public static void Error_Output_Failed_TypeCast()
		{
			BindHelper.NewProject
				.AddPou($"FUNCTION MyFunc VAR_OUTPUT arg : BOOL; END_VAR", "")
				.WithGlobalVar("x", "DINT")
				.BindGlobalExpression<FunctionCallBoundExpression>($"MyFunc(arg => x)", null, ErrorOfType<TypeIsNotConvertibleMessage>());
		}

		[Fact]
		public static void Error_Output_NotWritable()
		{
			BindHelper.NewProject
				.AddPou($"FUNCTION MyFunc VAR_OUTPUT arg : INT; END_VAR", "")
				.BindGlobalExpression<FunctionCallBoundExpression>($"MyFunc(arg => 5)", null, ErrorOfType<CannotAssignToSyntaxMessage>());
		}

		[Fact]
		public static void Error_InOut_NotWritable()
		{
			BindHelper.NewProject
				.AddPou($"FUNCTION MyFunc VAR_IN_OUT arg : INT; END_VAR", "")
				.BindGlobalExpression<FunctionCallBoundExpression>($"MyFunc(arg := 5)", null, ErrorOfType<CannotAssignToSyntaxMessage>());
		}

		[Fact]
		public static void Error_InOut_NotConvertible()
		{
			BindHelper.NewProject
				.AddPou($"FUNCTION MyFunc VAR_IN_OUT arg : INT; END_VAR", "")
				.WithGlobalVar("x", "DINT")
				.BindGlobalExpression<FunctionCallBoundExpression>($"MyFunc(arg := x)", null, ErrorOfType<InoutArgumentMustHaveSameTypeMessage>());
		}

		[Fact]
		public static void Error_CannotCallSyntax()
		{
			BindHelper.NewProject
				.BindGlobalExpression<FunctionCallBoundExpression>($"7()", null, ErrorOfType<CannotCallSyntaxMessage>());
		}

		[Fact]
		public static void Error_ParameterWasAlreadyPassed()
		{
			BindHelper.NewProject
				.AddPou($"FUNCTION MyFunc VAR_INPUT arg : INT; arg2 : INT; END_VAR", "")
				.WithGlobalVar("x", "INT")
				.BindGlobalExpression<FunctionCallBoundExpression>($"MyFunc(arg := x, arg := 6)", null, ErrorOfType<ParameterWasAlreadyPassedMessage>());
		}

		[Fact]
		public static void Error_MissingArgument()
		{
			BindHelper.NewProject
				.AddPou($"FUNCTION MyFunc VAR_INPUT arg : INT; arg2 : INT; END_VAR", "")
				.WithGlobalVar("x", "INT")
				.BindGlobalExpression<FunctionCallBoundExpression>($"MyFunc(arg := x)", null, ErrorOfType<WrongNumberOfArgumentsMessage>());
		}

		[Fact]
		public static void Error_PositionalArgumentAfterExplicit()
		{
			BindHelper.NewProject
				.AddPou($"FUNCTION MyFunc VAR_INPUT arg : INT; arg2 : INT; arg3 : INT; END_VAR", "")
				.WithGlobalVar("x", "INT")
				.BindGlobalExpression<FunctionCallBoundExpression>($"MyFunc(x, arg2 := 5, x)", null, ErrorOfType<CannotUsePositionalParameterAfterExplicitMessage>());
		}
		[Fact]
		public static void ExplicitArgumentDiffrentOrder()
		{
			var boundExpression = BindHelper.NewProject
				.AddPou($"FUNCTION MyFunc VAR_INPUT arg1 : INT; arg2 : INT; arg3 : INT; END_VAR", "")
				.BindGlobalExpression<FunctionCallBoundExpression>($"MyFunc(arg3 := 0, arg2 := 1, arg1 := 2)", null);
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
				.BindGlobalExpression<FunctionCallBoundExpression>($"MyFunc(unknownArg := 7)", null, ErrorOfType<ParameterNotFoundMessage>());
		}
		
		[Fact]
		public static void ExplicitReadReturnValue()
		{
			BindHelper.NewProject
				.AddPou($"FUNCTION MyFunc : INT VAR_INPUT x : INT; END_VAR", "")
				.WithGlobalVar("x", "INT")
				.BindGlobalExpression<FunctionCallBoundExpression>($"MyFunc(MyFunc => x)", null, ErrorOfType<ParameterNotFoundMessage>());
		}
	}
}
