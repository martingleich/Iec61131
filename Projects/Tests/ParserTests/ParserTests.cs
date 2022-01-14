using Compiler;
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
				LiteralExpressionSyntax(IntegerLiteralToken(1)))(parsed);
		}
		[Fact]
		public void ParseVarDeclStatement_NoType_NoInit()
		{
			var parsed = ParseStatements("VAR x;");
			StatementListSyntax(SyntaxArray<IStatementSyntax>(
				LocalVarDeclStatementSyntax("x".ToCaseInsensitive(), NullSyntax, NullSyntax)
				))(parsed);
		}
		[Fact]
		public void ParseVarDeclStatement_Type_NoInit()
		{
			var parsed = ParseStatements("VAR x : INT;");
			StatementListSyntax(SyntaxArray<IStatementSyntax>(
				LocalVarDeclStatementSyntax("x".ToCaseInsensitive(), VarTypeSyntax(BuiltInTypeSyntax(IntToken)), NullSyntax)
				))(parsed);
		}
		[Fact]
		public void ParseVarDeclStatement_Type_Init()
		{
			var parsed = ParseStatements("VAR x : INT := 0;");
			StatementListSyntax(SyntaxArray<IStatementSyntax>(
				LocalVarDeclStatementSyntax("x".ToCaseInsensitive(), VarTypeSyntax(BuiltInTypeSyntax(IntToken)), VarInitSyntax(LiteralExpressionSyntax(IntegerLiteralToken(0))))
				))(parsed);
		}
		[Fact]
		public void ParseVarDeclStatement_NoType_Init()
		{
			var parsed = ParseStatements("VAR x := 0;");
			StatementListSyntax(SyntaxArray<IStatementSyntax>(
				LocalVarDeclStatementSyntax("x".ToCaseInsensitive(), NullSyntax, VarInitSyntax(LiteralExpressionSyntax(IntegerLiteralToken(0))))
				))(parsed);
		}
	}
}
