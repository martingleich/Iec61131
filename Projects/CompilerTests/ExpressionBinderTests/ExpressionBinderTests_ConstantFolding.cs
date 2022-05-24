using Compiler;
using Compiler.Messages;
using Xunit;

namespace CompilerTests.ExpressionBinderTests
{
	public class ExpressionBinderTests_ConstantFolding
	{
		[Theory]
		[InlineData("BOOL#TRUE AND BOOL#TRUE", null, "BOOL#TRUE")]
		[InlineData("BOOL#FALSE AND BOOL#TRUE", null, "BOOL#FALSE")]
		[InlineData("BOOL#TRUE OR BOOL#TRUE", null, "BOOL#TRUE")]
		[InlineData("BOOL#FALSE OR BOOL#FALSE", null, "BOOL#FALSE")]
		[InlineData("BOOL#FALSE OR BOOL#TRUE", null, "BOOL#TRUE")]

		[InlineData("SINT#34 + SINT#12", null, "SINT#46")]
		[InlineData("SINT#100 + SINT#100", null, null)]
		[InlineData("SINT#12 - SINT#7", null, "SINT#5")]
		[InlineData("-SINT#60 - SINT#100", null, null)]
		[InlineData("SINT#3 * SINT#4", null, "SINT#12")]
		[InlineData("SINT#100 * SINT#100", null, null)]
		[InlineData("SINT#5 / SINT#2", null, "SINT#2")]
		[InlineData("SINT#5 / SINT#0", null, null)]
		[InlineData("SINT#5 MOD SINT#2", null, "SINT#1")]
		[InlineData("SINT#5 MOD SINT#0", null, null)]
		[InlineData("-SINT#5", null, "-SINT#5")]

		[InlineData("USINT#34 + USINT#12", null, "USINT#46")]
		[InlineData("USINT#212 + USINT#132", null, null)]
		[InlineData("USINT#12 - USINT#7", null, "USINT#5")]
		[InlineData("USINT#0 - USINT#100", null, null)]
		[InlineData("USINT#3 * USINT#4", null, "USINT#12")]
		[InlineData("USINT#100 * USINT#100", null, null)]
		[InlineData("USINT#5 / USINT#2", null, "USINT#2")]
		[InlineData("USINT#5 / USINT#0", null, null)]
		[InlineData("USINT#5 MOD USINT#2", null, "USINT#1")]
		[InlineData("USINT#5 MOD USINT#0", null, null)]

		[InlineData("INT#1619 + INT#1507", null, "INT#3126")]
		[InlineData("INT#20000 + INT#20000", null, null)]
		[InlineData("INT#12 - INT#7", null, "INT#5")]
		[InlineData("-INT#20000 - INT#20000", null, null)]
		[InlineData("INT#32 * INT#174", null, "INT#5568")]
		[InlineData("INT#23534 * INT#23514", null, null)]
		[InlineData("INT#5 / INT#2", null, "INT#2")]
		[InlineData("INT#5 / INT#0", null, null)]
		[InlineData("INT#5 MOD INT#2", null, "INT#1")]
		[InlineData("INT#5 MOD INT#0", null, null)]
		[InlineData("-INT#5", null, "-INT#5")]

		[InlineData("UINT#1619 + UINT#1507", null, "UINT#3126")]
		[InlineData("UINT#41465 + UINT#34125", null, null)]
		[InlineData("UINT#12 - UINT#7", null, "UINT#5")]
		[InlineData("UINT#0 - UINT#20000", null, null)]
		[InlineData("UINT#32 * UINT#174", null, "UINT#5568")]
		[InlineData("UINT#23534 * UINT#23514", null, null)]
		[InlineData("UINT#5 / UINT#2", null, "UINT#2")]
		[InlineData("UINT#5 / UINT#0", null, null)]
		[InlineData("UINT#5 MOD UINT#2", null, "UINT#1")]
		[InlineData("UINT#5 MOD UINT#0", null, null)]

		[InlineData("DINT#1619 + DINT#1507", null, "DINT#3126")]
		[InlineData("DINT#2142433645 + DINT#2143483642", null, null)]
		[InlineData("DINT#12 - DINT#7", null, "DINT#5")]
		[InlineData("-DINT#2142433645 - DINT#2143483642", null, null)]
		[InlineData("DINT#32 * DINT#174", null, "DINT#5568")]
		[InlineData("DINT#23534254 * DINT#23534214", null, null)]
		[InlineData("DINT#5 / DINT#2", null, "DINT#2")]
		[InlineData("DINT#5 / DINT#0", null, null)]
		[InlineData("DINT#5 MOD DINT#2", null, "DINT#1")]
		[InlineData("DINT#5 MOD DINT#0", null, null)]
		[InlineData("-DINT#5", null, "-DINT#5")]

		[InlineData("UDINT#1619 + UDINT#1507", null, "UDINT#3126")]
		[InlineData("UDINT#4146543234 + UDINT#341252342", null, null)]
		[InlineData("UDINT#12 - UDINT#7", null, "UDINT#5")]
		[InlineData("UDINT#0 - UDINT#20000", null, null)]
		[InlineData("UDINT#32 * UDINT#174", null, "UDINT#5568")]
		[InlineData("UDINT#23534435 * UDINT#23213514", null, null)]
		[InlineData("UDINT#5 / UDINT#2", null, "UDINT#2")]
		[InlineData("UDINT#5 / UDINT#0", null, null)]
		[InlineData("UDINT#5 MOD UDINT#2", null, "UDINT#1")]
		[InlineData("UDINT#5 MOD UDINT#0", null, null)]


		[InlineData("LINT#1619 + LINT#1507", null, "LINT#3126")]
		[InlineData("LINT#9223372035973208853 + LINT#9223372035973208853", null, null)]
		[InlineData("LINT#12 - LINT#7", null, "LINT#5")]
		[InlineData("-LINT#9223372035973208853 - LINT#9223372035973208853", null, null)]
		[InlineData("LINT#32 * LINT#174", null, "LINT#5568")]
		[InlineData("LINT#23534224654 * LINT#223643534214", null, null)]
		[InlineData("LINT#5 / LINT#2", null, "LINT#2")]
		[InlineData("LINT#5 / LINT#0", null, null)]
		[InlineData("LINT#5 MOD LINT#2", null, "LINT#1")]
		[InlineData("LINT#5 MOD LINT#0", null, null)]
		[InlineData("-LINT#5", null, "-LINT#5")]

		[InlineData("ULINT#1619 + ULINT#1507", null, "ULINT#3126")]
		[InlineData("ULINT#18446744073709551615 + ULINT#18446744073709551615", null, null)]
		[InlineData("ULINT#12 - ULINT#7", null, "ULINT#5")]
		[InlineData("ULINT#0 - ULINT#20000", null, null)]
		[InlineData("ULINT#32 * ULINT#174", null, "ULINT#5568")]
		[InlineData("ULINT#215423533534435 * ULINT#232134215514", null, null)]
		[InlineData("ULINT#5 / ULINT#2", null, "ULINT#2")]
		[InlineData("ULINT#5 / ULINT#0", null, null)]
		[InlineData("ULINT#5 MOD ULINT#2", null, "ULINT#1")]
		[InlineData("ULINT#5 MOD ULINT#0", null, null)]

		[InlineData("REAL#5 + REAL#0", null, null)]
		[InlineData("REAL#5", "LREAL", "LREAL#5")]
		public void Constant(string expression, string targetType, string literalResult)
		{
			var (boundExpression, boundItf) = BindHelper.NewProject
				.BindGlobalExpressionEx<IBoundExpression>(expression, targetType);
			if (literalResult != null)
			{
				var (boundExpressionExpected, _) = BindHelper.NewProject
					.BindGlobalExpressionEx<IBoundExpression>(literalResult, null);

				AssertEx.HasConstantValue(boundExpression, boundItf.SystemScope, valueCur =>
					AssertEx.HasConstantValue(boundExpressionExpected, boundItf.SystemScope, valueExpected => Assert.Equal(valueExpected, valueCur)));
			}
			else
			{
				var bag = new MessageBag();
				var result = ConstantExpressionEvaluator.EvaluateConstant(boundItf.SystemScope, boundExpression, bag);
				Assert.NotEmpty(bag);
				Assert.Null(result);
			}
		}

		[Fact]
		public void NotAConstantVariable()
		{
			var (boundExpression, boundItf) = BindHelper.NewProject
				.WithGlobalVar("x", "INT")
				.BindGlobalExpressionEx<IBoundExpression>("x", null);
			AssertEx.NotAConstant(boundExpression, boundItf.SystemScope);
		}
	}
}
