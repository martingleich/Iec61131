﻿using Compiler;
using Compiler.Messages;
using Compiler.Types;
using Xunit;

namespace Tests
{
	using static ErrorTestHelper;
	public static class ExpressionBinderTests
	{
		private static readonly SystemScope SystemScope = SystemScope.Instance;
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
				// Typedliteral
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
					.BindGlobalExpression(value, null, ErrorOfType<ConstantDoesNotFitIntoType>());
			}

			[Fact]
			public static void Int_NoTargetType_ToBig()
			{
				BindHelper.NewProject
					.BindGlobalExpression("99999999999999999999999999999999999999", null, ErrorOfType<ConstantDoesNotFitIntoAnyType>());
			}

			[Fact]
			public static void Pointer_ZeroAsPointer()
			{
				var boundExpression = BindHelper.NewProject
					.BindGlobalExpression("0", new PointerType(SystemScope.Int));
				AssertEx.EqualType(new PointerType(SystemScope.Int), boundExpression.Type);
			}
			[Fact]
			public static void Error_Pointer_OneAsPointer()
			{
				BindHelper.NewProject
					.BindGlobalExpression("1", new PointerType(SystemScope.Int), ErrorOfType<ConstantDoesNotFitIntoType>());
			}
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
	}
}
