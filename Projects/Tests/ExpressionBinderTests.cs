using Compiler;
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

			[Fact]
			public static void Int_NoTargetType_ToBig()
			{
				BindHelper.NewProject
					.BindGlobalExpression("99999999999999999999999999999999999999", null, ErrorOfType<ConstantDoesNotFitIntoAnyType>());
			}
			[Fact]
			public static void Bool_NoTargetType_InvalidConstant()
			{
				BindHelper.NewProject
					.BindGlobalExpression("BOOL#8", null, ErrorOfType<ConstantDoesNotFitIntoType>(msg => Assert.Equal(msg.TargetType, SystemScope.Bool)));
			}
		}
	}
}
