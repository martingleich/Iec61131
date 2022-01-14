using Compiler;
using Compiler.Messages;
using Xunit;

namespace Tests
{
	using static ParserTestHelper;
	using static ScannerTestHelper;
	using static ErrorHelper;

	public static class ParserTests_ScopingOperator
	{
		[Fact]
		public static void Unscoped()
		{
			var parsed = ParseExpression("x");
			VariableExpressionSyntax("x")(parsed);
		}
		[Fact]
		public static void SingleScope()
		{
			var parsed = ParseExpression("a::x");
			ScopedVariableExpressionSyntax(
				ScopeQualifierSyntax(NullSyntax, "a".ToCaseInsensitive()),
				IdentifierToken("x"))(parsed);
		}
		[Fact]
		public static void TwoScopes()
		{
			var parsed = ParseExpression("a::b::x");
			ScopedVariableExpressionSyntax(
				ScopeQualifierSyntax(
					ScopeQualifierSyntax(NullSyntax, "a".ToCaseInsensitive()), "b".ToCaseInsensitive()),
				IdentifierToken("x"))(parsed);
		}
		[Fact]
		public static void Error_MissingVariable()
		{
			var parsed = ParseExpression("a::", ErrorOfType<ExpectedSyntaxMessage>());
			ScopedVariableExpressionSyntax(
				ScopeQualifierSyntax(NullSyntax, "a".ToCaseInsensitive()),
				IdentifierToken("__ERROR__"))(parsed);
		}
		[Fact]
		public static void Error_MissingScope()
		{
			var parsed = ParseExpression("::x", ErrorOfType<ExpectedExpressionMessage>(), ErrorOfType<ExpectedSyntaxMessage>());
			VariableExpressionSyntax("__ERROR__")(parsed);
		}
	}
}
