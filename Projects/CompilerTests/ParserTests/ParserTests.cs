using Compiler;
using Xunit;

namespace CompilerTests
{
	using static ScannerTestHelper;
	using static ParserTestHelper;
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
		[Fact]
		public void ParseVarDeclForStatement_NoType()
		{
			var parsed = ParseStatements("FOR VAR x := 0 TO 10 DO END_FOR");
			StatementListSyntax(SyntaxArray<IStatementSyntax>(
				ForStatementSyntax(
					ForStatementDeclareLocalIndexSyntax("x".ToCaseInsensitive(), NullSyntax, VarInitSyntax(LiteralExpressionSyntax(IntegerLiteralToken(0)))),
					LiteralExpressionSyntax(IntegerLiteralToken(10)),
					NullSyntax,
					StatementListSyntax(SyntaxArray<IStatementSyntax>()))
				))(parsed);
		}
		[Fact]
		public void ParseVarDeclForStatement_Type()
		{
			var parsed = ParseStatements("FOR VAR x : INT := 0 TO 10 DO END_FOR");
			StatementListSyntax(SyntaxArray<IStatementSyntax>(
				ForStatementSyntax(
					ForStatementDeclareLocalIndexSyntax(
						"x".ToCaseInsensitive(),
						VarTypeSyntax(BuiltInTypeSyntax(IntToken)),
						VarInitSyntax(LiteralExpressionSyntax(IntegerLiteralToken(0)))),
					LiteralExpressionSyntax(IntegerLiteralToken(10)),
					NullSyntax,
					StatementListSyntax(SyntaxArray<IStatementSyntax>()))
				))(parsed);
		}
	}
}
