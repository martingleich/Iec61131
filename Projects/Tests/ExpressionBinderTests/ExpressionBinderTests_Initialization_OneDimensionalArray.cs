using Compiler;
using Compiler.Messages;
using Xunit;

namespace Tests.ExpressionBinderTests
{
	using static ErrorHelper;
	using static BindHelper;

	public static class ExpressionBinderTests_Initialization_OneDimensionalArray
	{
		[Fact]
		public static void EmptyArray()
		{
			var boundExpression = BindHelper.NewProject
				.BindGlobalExpression("{}", "ARRAY[0..-1] OF INT");
			var init = Assert.IsType<InitializerBoundExpression>(boundExpression);
			Assert.Empty(init.Elements);
			AssertEx.EqualCaseInsensitive("ARRAY[0..-1] OF INT", init.Type.Code);
		}
		[Fact]
		public static void InitIndices()
		{
			var boundExpression = BindHelper.NewProject
				.BindGlobalExpression("{[0] := 1, [1] := 2, [2] := 3}", "ARRAY[0..2] OF INT");
			var init = Assert.IsType<InitializerBoundExpression>(boundExpression);
			Assert.Collection(init.Elements,
				ArrayElement(0, BoundIntLiteral(1)),
				ArrayElement(1, BoundIntLiteral(2)),
				ArrayElement(2, BoundIntLiteral(3)));
		}
		[Fact]
		public static void InitIndices_Unordered()
		{
			var boundExpression = BindHelper.NewProject
				.BindGlobalExpression("{[2] := 7, [1] := 6, [0] := 3}", "ARRAY[0..2] OF INT");
			var init = Assert.IsType<InitializerBoundExpression>(boundExpression);
			Assert.Collection(init.Elements,
				ArrayElement(2, BoundIntLiteral(7)),
				ArrayElement(1, BoundIntLiteral(6)),
				ArrayElement(0, BoundIntLiteral(3)));
		}
		[Fact]
		public static void InitAll()
		{
			var boundExpression = BindHelper.NewProject
				.BindGlobalExpression("{[..] := 7}", "ARRAY[0..2] OF INT");
			var init = Assert.IsType<InitializerBoundExpression>(boundExpression);
			Assert.Collection(init.Elements,
				AllElements(BoundIntLiteral(7)));
		}
		[Fact]
		public static void Implicit()
		{
			var boundExpression = BindHelper.NewProject
				.BindGlobalExpression("{1, 2, 3}", "ARRAY[0..2] OF INT");
			var init = Assert.IsType<InitializerBoundExpression>(boundExpression);
			Assert.Collection(init.Elements,
				ArrayElement(0, BoundIntLiteral(1)),
				ArrayElement(1, BoundIntLiteral(2)),
				ArrayElement(2, BoundIntLiteral(3)));
		}
		[Fact]
		public static void ComplexIndex()
		{
			var boundExpression = BindHelper.NewProject
				.BindGlobalExpression("{[5 - (2*2) - 1] := 6}", "ARRAY[0..0] OF INT");
			var init = Assert.IsType<InitializerBoundExpression>(boundExpression);
			Assert.Collection(init.Elements,
				ArrayElement(0, BoundIntLiteral(6)));
		}
		[Fact]
		public static void ComplexValue()
		{
			var boundExpression = BindHelper.NewProject
				.WithGlobalVar("localVar", "INT")
				.BindGlobalExpression("{[0] := localVar}", "ARRAY[0..0] OF INT");
			var init = Assert.IsType<InitializerBoundExpression>(boundExpression);
			Assert.Collection(init.Elements,
				ArrayElement(0, BoundVariable("localVar")));
		}
		[Fact]
		public static void AliasToArray()
		{
			var boundExpression = BindHelper.NewProject
				.AddDutFast("MyAlias", "ARRAY[0..0] OF INT")
				.BindGlobalExpression("{[0] := 1}", "MyAlias");
			var cast = Assert.IsType<ImplicitAliasFromBaseTypeCastBoundExpression>(boundExpression);
			var init = Assert.IsType<InitializerBoundExpression>(cast.Value);
			Assert.Collection(init.Elements,
				ArrayElement(0, BoundIntLiteral(1)));
		}
		[Fact]
		public static void Error_ToManyImplicit()
		{
			BindHelper.NewProject
				.BindGlobalExpression("{1, 2, 3, 4}", "ARRAY[0..2] OF INT", ErrorOfType<TypeDoesNotHaveThisElementMessage>());
		}
		[Fact]
		public static void Error_ToFewImplicit()
		{
			BindHelper.NewProject
				.BindGlobalExpression("{1, 2}", "ARRAY[0..2] OF INT", ErrorOfType<IndexNotInitializedMessage>(msg => Assert.Equal(2, msg.Index)));
		}
		[Fact]
		public static void Error_ToFewExplicit()
		{
			BindHelper.NewProject
				.BindGlobalExpression("{[0] := 1, [2] := 2}", "ARRAY[0..2] OF INT", ErrorOfType<IndexNotInitializedMessage>(msg => Assert.Equal(1, msg.Index)));
		}
		[Fact]
		public static void Error_UnknownImplicit()
		{
			BindHelper.NewProject
				.BindGlobalExpression("{[0] := 1, [1] := 2, [2] := 3, [-1] := 8}", "ARRAY[0..2] OF INT", ErrorOfType<TypeDoesNotHaveThisElementMessage>());
		}
		[Fact]
		public static void Error_FieldElement()
		{
			BindHelper.NewProject
				.BindGlobalExpression("{.field := 1}", "ARRAY[0..-1] OF INT", ErrorOfType<TypeDoesNotHaveThisElementMessage>());
		}
		[Fact]
		public static void Error_DuplicateExplicitIndex()
		{
			BindHelper.NewProject
				.BindGlobalExpression("{1, [0] := 2}", "ARRAY[0..0] OF INT", ErrorOfType<DuplicateInitializerElementMessage>());
		}
		[Fact]
		public static void Error_DuplicateAllIndex()
		{
			BindHelper.NewProject
				.BindGlobalExpression("{1, [..] := 2}", "ARRAY[0..0] OF INT", ErrorOfType<DuplicateInitializerElementMessage>());
		}
		[Fact]
		public static void Error_ImplicitAfterExplicit_Index()
		{
			BindHelper.NewProject
				.BindGlobalExpression("{[0] := 2, 7}", "ARRAY[0..0] OF INT", ErrorOfType<CannotUsePositionalElementAfterExplicitMessage>());
		}
		[Fact]
		public static void Error_ImplicitAfterExplicit_All()
		{
			BindHelper.NewProject
				.BindGlobalExpression("{[..] := 2, 7}", "ARRAY[0..0] OF INT", ErrorOfType<CannotUsePositionalElementAfterExplicitMessage>());
		}
		[Fact]
		public static void Error_ImplicitAfterExplicit_All_NoDuplicateMessage()
		{
			BindHelper.NewProject
				.BindGlobalExpression("{[..] := 2, 7, 0}", "ARRAY[0..0] OF INT", ErrorOfType<CannotUsePositionalElementAfterExplicitMessage>());
		}
	}
}
