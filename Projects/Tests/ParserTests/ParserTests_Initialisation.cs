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
			InitializationExpressionSyntax(SyntaxCommaSeparated<IInitializerElementSyntax>())(expr);
		}

		[Fact]
		public void SingleExpressionValue()
		{
			var expr = ParseExpression("{x+y}");
			InitializationExpressionSyntax(SyntaxCommaSeparated<IInitializerElementSyntax>(
				ImplicitInitializerElementSyntax(BinaryOperatorExpressionSyntax(
					VariableExpressionSyntax("x"),
					PlusToken,
					VariableExpressionSyntax("y")))))(expr);
		}
		[Fact]
		public void TwoExpressionValues()
		{
			var expr = ParseExpression("{x+y, 8}");
			InitializationExpressionSyntax(SyntaxCommaSeparated<IInitializerElementSyntax>(
				ImplicitInitializerElementSyntax(BinaryOperatorExpressionSyntax(
					VariableExpressionSyntax("x"),
					PlusToken,
					VariableExpressionSyntax("y"))),
				ImplicitInitializerElementSyntax(LiteralExpressionSyntax(IntegerLiteralToken(8)))))(expr);
		}
		[Fact]
		public void RecursiveExpressionValue()
		{
			var expr = ParseExpression("{{x+y}, 8}");
			InitializationExpressionSyntax(SyntaxCommaSeparated<IInitializerElementSyntax>(
				ImplicitInitializerElementSyntax(
				InitializationExpressionSyntax(SyntaxCommaSeparated<IInitializerElementSyntax>(
					ImplicitInitializerElementSyntax(BinaryOperatorExpressionSyntax(
						VariableExpressionSyntax("x"),
						PlusToken,
						VariableExpressionSyntax("y")))))),
				ImplicitInitializerElementSyntax(LiteralExpressionSyntax(IntegerLiteralToken(8)))))(expr);
		}

		[Fact]
		public void SingleArrayIndexAssignExpression()
		{
			var expr = ParseExpression("{[1 - 2] := z}");
			InitializationExpressionSyntax(SyntaxCommaSeparated<IInitializerElementSyntax>(
				ExplicitInitializerElementSyntax(
					IndexElementSyntax(
						BinaryOperatorExpressionSyntax(
							LiteralExpressionSyntax(IntegerLiteralToken(1)),
							MinusToken,
							LiteralExpressionSyntax(IntegerLiteralToken(2)))),
					VariableExpressionSyntax(IdentifierToken("z")))))(expr);
		}
		[Fact]
		public void FieldAssignExpression()
		{
			var expr = ParseExpression("{.field := z}");
			InitializationExpressionSyntax(SyntaxCommaSeparated<IInitializerElementSyntax>(
				ExplicitInitializerElementSyntax(
					FieldElementSyntax(
						"field".ToCaseInsensitive()),
					VariableExpressionSyntax(IdentifierToken("z")))))(expr);
		}
		[Fact]
		public void AllAssignExpression()
		{
			var expr = ParseExpression("{[..] := z}");
			InitializationExpressionSyntax(SyntaxCommaSeparated<IInitializerElementSyntax>(
				ExplicitInitializerElementSyntax(
					AllIndicesElementSyntax(),
					VariableExpressionSyntax(IdentifierToken("z")))))(expr);
		}
		[Fact]
		public void TypedExpressionSyntax_Userdef()
		{
			var expr = ParseExpression("MyType#{x}");
			TypedInitializationExpressionSyntax(
				IdentifierTypeSyntax(IdentifierToken("MyType")),
					InitializationExpressionSyntax(SyntaxCommaSeparated<IInitializerElementSyntax>(
						ImplicitInitializerElementSyntax(VariableExpressionSyntax(IdentifierToken("x"))))))(expr);
		}
		[Fact]
		public void TypedExpressionSyntax_Userdef_Qualified()
		{
			var expr = ParseExpression("Qualifier::MyType#{}");
			TypedInitializationExpressionSyntax(
				ScopedIdentifierTypeSyntax(ScopeQualifierSyntax(NullSyntax, "Qualifier".ToCaseInsensitive()), "MyType".ToCaseInsensitive()),
				InitializationExpressionSyntax(SyntaxCommaSeparated<IInitializerElementSyntax>()))(expr);
		}
		[Fact]
		public void TypedExpressionSyntax_Array()
		{
			var expr = ParseExpression("ARRAY[0..10] OF INT#{}");
			TypedInitializationExpressionSyntax(
				ArrayTypeSyntax(
					SyntaxCommaSeparated<RangeSyntax>(RangeSyntax(
						LiteralExpressionSyntax(IntegerLiteralToken(0)),
						LiteralExpressionSyntax(IntegerLiteralToken(10)))),
					BuiltInTypeSyntax(IntToken)),
				InitializationExpressionSyntax(SyntaxCommaSeparated<IInitializerElementSyntax>()))(expr);
		}
	}
}
