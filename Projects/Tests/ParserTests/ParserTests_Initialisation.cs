using Compiler;
using Xunit;

namespace Tests
{
	using static ParserTestHelper;
	using static ScannerTestHelper;

	public class ParserTests_Initialisation
	{
		[Fact]
		public void ParseEmpty()
		{
			var expr = ParseExpression("{}");
			InitializationExpressionSyntax(SyntaxCommaSeperated<IInitializerElementSyntax>())(expr);
		}

		[Fact]
		public void SingleExpressionValue()
		{
			var expr = ParseExpression("{x+y}");
			InitializationExpressionSyntax(SyntaxCommaSeperated<IInitializerElementSyntax>(
				ExpressionElementSyntax(BinaryOperatorExpressionSyntax(
					VariableExpressionSyntax("x"),
					PlusToken,
					VariableExpressionSyntax("y")))))(expr);
		}
		[Fact]
		public void TwoExpressionValues()
		{
			var expr = ParseExpression("{x+y, 8}");
			InitializationExpressionSyntax(SyntaxCommaSeperated<IInitializerElementSyntax>(
				ExpressionElementSyntax(BinaryOperatorExpressionSyntax(
					VariableExpressionSyntax("x"),
					PlusToken,
					VariableExpressionSyntax("y"))),
				ExpressionElementSyntax(LiteralExpressionSyntax(IntegerLiteralToken(8)))))(expr);
		}
		[Fact]
		public void RecursiveExpressionValue()
		{
			var expr = ParseExpression("{{x+y}, 8}");
			InitializationExpressionSyntax(SyntaxCommaSeperated<IInitializerElementSyntax>(
				ExpressionElementSyntax(
				InitializationExpressionSyntax(SyntaxCommaSeperated<IInitializerElementSyntax>(
					ExpressionElementSyntax(BinaryOperatorExpressionSyntax(
						VariableExpressionSyntax("x"),
						PlusToken,
						VariableExpressionSyntax("y")))))),
				ExpressionElementSyntax(LiteralExpressionSyntax(IntegerLiteralToken(8)))))(expr);
		}

		[Fact]
		public void SingleArrayIndexAssignExpression()
		{
			var expr = ParseExpression("{[1 - 2] := z}");
			InitializationExpressionSyntax(SyntaxCommaSeperated<IInitializerElementSyntax>(
				IndexInitializerElementSyntax(
					BinaryOperatorExpressionSyntax(
						LiteralExpressionSyntax(IntegerLiteralToken(1)),
						MinusToken,
						LiteralExpressionSyntax(IntegerLiteralToken(2))),
					VariableExpressionSyntax(IdentifierToken("z")))))(expr);
		}
		[Fact]
		public void FieldAssignExpression()
		{
			var expr = ParseExpression("{.field := z}");
			InitializationExpressionSyntax(SyntaxCommaSeperated<IInitializerElementSyntax>(
				FieldInitializerElementSyntax(
					"field".ToCaseInsensitive(),
					VariableExpressionSyntax(IdentifierToken("z")))))(expr);
		}
		[Fact]
		public void AllAssignExpression()
		{
			var expr = ParseExpression("{[..] := z}");
			InitializationExpressionSyntax(SyntaxCommaSeperated<IInitializerElementSyntax>(
				AllIndicesInitializerElementSyntax(
					VariableExpressionSyntax(IdentifierToken("z")))))(expr);
		}
	}
}
