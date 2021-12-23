using Xunit;

namespace Tests
{
	using static ParserTestHelper;
	using static ScannerTestHelper;

	public sealed class ParserTests
	{
		[Fact]
		public void ParseStrangeAssign()
		{
			var parsed = ParseStatements("(x + 5) := y;");
			StatementListSyntax(SyntaxArray<Compiler.IStatementSyntax>(
				AssignStatementSyntax(
					ParenthesisedExpressionSyntax(
						BinaryOperatorExpressionSyntax(
							VariableExpressionSyntax("x"),
							PlusToken,
							LiteralExpressionSyntax(IntegerLiteralToken(5)))),
					VariableExpressionSyntax("y"))))(parsed);
		}
		[Fact]
		public void ParseCompactSubtraction()
		{
			var parsed = ParseExpression("x-1");
			BinaryOperatorExpressionSyntax(
				VariableExpressionSyntax("x"),
				MinusToken,
				LiteralExpressionSyntax(IntegerLiteralToken(1)));
		}
	}
}
