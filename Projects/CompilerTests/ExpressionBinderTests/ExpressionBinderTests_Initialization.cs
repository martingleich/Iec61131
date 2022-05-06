using Compiler.Messages;
using Xunit;

namespace Tests.ExpressionBinderTests
{
	using static ErrorHelper;

	public static class ExpressionBinderTests_Initialization
	{
		[Fact]
		public static void Error_MissingType()
		{
			BindHelper.NewProject
				.BindGlobalExpression("{}", null, ErrorOfType<CannotInferTypeForInitializerMessage>());
		}
		[Fact]
		public static void Error_UnsupportedType_INT()
		{
			BindHelper.NewProject
				.BindGlobalExpression("{}", "INT", ErrorOfType<CannotUseAnInitializerForThisTypeMessage>());
		}
	}
}
