using Compiler;
using Compiler.Messages;
using Xunit;

namespace CompilerTests.ExpressionBinderTests
{
	using static ErrorHelper;
	using static BindHelper;

	public static class ExpressionBinderTests_Initialization_MultiDimensionalArray
	{
		[Fact]
		public static void EmptyArray()
		{
			var boundExpression = BindHelper.NewProject
				.BindGlobalExpression("{}", "ARRAY[0..-1, 0..-1] OF INT");
			var init = Assert.IsType<InitializerBoundExpression>(boundExpression);
			Assert.Empty(init.Elements);
			AssertEx.EqualCaseInsensitive("ARRAY[0..-1, 0..-1] OF INT", init.Type.Code);
		}
		[Fact]
		public static void InitAll()
		{
			var boundExpression = BindHelper.NewProject
				.BindGlobalExpression("{[..] := 123}", "ARRAY[0..2, 0..2] OF INT");
			var init = Assert.IsType<InitializerBoundExpression>(boundExpression);
			Assert.Collection(init.Elements,
				AllElements(BoundIntLiteral(123)));
		}
		[Fact]
		public static void Error_DuplicateElement()
		{
			BindHelper.NewProject
				.BindGlobalExpression("{[..] := 123, [..] := 456}", "ARRAY[0..2, 0..2] OF INT", ErrorOfType<DuplicateInitializerElementMessage>());
		}
		[Fact]
		public static void Error_MissingElement()
		{
			BindHelper.NewProject
				.BindGlobalExpression("{}", "ARRAY[0..2, 0..2] OF INT", ErrorOfType<MissingElementsInInitializerMessage>());
		}
		[Fact]
		public static void Error_FieldElement()
		{
			BindHelper.NewProject
				.BindGlobalExpression("{.field := 1}", "ARRAY[0..-1, 0..-1] OF INT", ErrorOfType<TypeDoesNotHaveThisElementMessage>());
		}
		[Fact]
		public static void Error_IndexElement()
		{
			BindHelper.NewProject
				.BindGlobalExpression("{[0] := 1}", "ARRAY[0..-1, 0..-1] OF INT", ErrorOfType<TypeDoesNotHaveThisElementMessage>());
		}
		[Fact]
		public static void Error_Implicit()
		{
			BindHelper.NewProject
				.BindGlobalExpression("{0}", "ARRAY[0..-1, 0..-1] OF INT", ErrorOfType<CannotUseImplicitInitializerForThisTypeMessage>());
		}
	}
}
