using Compiler.Messages;
using System;
using Xunit;

namespace Tests
{
	using static ScannerTestHelper;
	using static ErrorTestHelper;
	public class ScannerTests
	{
		[Fact]
		public void EmptyInput()
		{
			AssertAllTokens("");
		}

		[Fact]
		public void WhitespaceInput()
		{
			AssertAllTokens(" ",
				EndToken.Leading(WhitespaceToken(" ").Generating(" ").Position(0, 1)).Trailing(Assert.Null));
		}
		[Fact]
		public void MixedWhitespace()
		{
			AssertAllTokens(" \t\n ",
				EndToken.Leading(WhitespaceToken(" \t\n ").Generating(" \t\n ")));
		}
		[Fact]
		public void IntegerBare()
		{
			AssertAllTokens("1234",
				IntegerLiteralToken(1234).Generating("1234").Position(0, 4));
		}
		[Fact]
		public void IntegerLeading()
		{
			AssertAllTokens("\t1234",
				IntegerLiteralToken(1234).Generating("1234").Position(1, 4).Leading(WhitespaceToken("\t")));
		}
		[Fact]
		public void IntegerTrailing()
		{
			AssertAllTokens("1234\t",
				IntegerLiteralToken(1234).Generating("1234").Position(0, 4).Trailing(WhitespaceToken("\t")));
		}
		[Fact]
		public void OverflowingInteger()
		{
			AssertAllTokens("99999999999999999999999999",
				IntegerLiteralToken(Compiler.OverflowingInteger.Overflown).Generating("99999999999999999999999999"));
		}
		[Fact]
		public void RealBare()
		{
			AssertAllTokens("3.1415",
				RealLiteralToken(3.1415).Generating("3.1415"));
		}
		[Fact]
		public void RealBare_WithUnderscore()
		{
			AssertAllTokens("1_003.1415",
				RealLiteralToken(1003.1415).Generating("1_003.1415"));
		}
		[Fact]
		public void OverflowingReal()
		{
			var str = new string('9', 1000) + ".0";
			AssertAllTokens(str,
				RealLiteralToken(Compiler.OverflowingReal.Overflown).Generating(str));
		}
		[Fact]
		public void BoolTrue()
		{
			AssertAllTokens("BOOL#TRUE", TypedLiteralToken(BoolToken, TrueToken));
		}
		[Fact]
		public void BoolFalse()
		{
			AssertAllTokens("BOOL#FALSE", TypedLiteralToken(BoolToken, FalseToken));
		}
		[Fact]
		public void Bool0()
		{
			AssertAllTokens("BOOL#0", TypedLiteralToken(BoolToken, IntegerLiteralToken(0)));
		}
		[Fact]
		public void Bool1()
		{
			AssertAllTokens("BOOL#1", TypedLiteralToken(BoolToken, IntegerLiteralToken(1)));
		}
		[Fact]
		public void Bool17()
		{
			AssertAllTokens("BOOL#17", TypedLiteralToken(BoolToken, IntegerLiteralToken(17)));
		}
		[Fact]
		public void Attribute()
		{
			AssertAllTokens("{My attribute 1234 &/-}", AttributeToken("My attribute 1234 &/-"));
		}
		[Fact]
		public void Attribute_Empty()
		{
			AssertAllTokens("{}", AttributeToken(""));
		}
		[Fact]
		public void Attribute_MissingEnd()
		{
			AssertAllTokens_WithError("{My attribute 1234 &/-",
				ExactlyMessages(ErrorOfType<Compiler.Messages.MissingEndOfAttributeMessage>()),
				AttributeToken("My attribute 1234 &/-"));
		}
		[Fact]
		public void Attribute_MissingEnd_Empty()
		{
			AssertAllTokens_WithError("{",
				ExactlyMessages(ErrorOfType<Compiler.Messages.MissingEndOfAttributeMessage>()),
				AttributeToken(""));
		}
		[Fact]
		public void Integer_Simple()
		{
			AssertAllTokens("123", IntegerLiteralToken(123));
		}
		[Fact]
		public void Integer_Simple__WithUnderscore()
		{
			AssertAllTokens("1234", IntegerLiteralToken(1234));
		}
		[Fact]
		public void Integer_SpacedSign()
		{
			AssertAllTokens("- 1234", MinusToken, IntegerLiteralToken(1234));
		}
		[Theory]
		[InlineData("INT")]
		[InlineData("UINT")]
		[InlineData("SINT")]
		[InlineData("USINT")]
		[InlineData("DINT")]
		[InlineData("UDINT")]
		[InlineData("LINT")]
		[InlineData("ULINT")]
		[InlineData("BYTE")]
		[InlineData("WORD")]
		[InlineData("DWORD")]
		[InlineData("LWORD")]
		[InlineData("REAL")]
		[InlineData("LREAL")]
		public void TypeQualifiedInteger(string type)
		{
			AssertAllTokens(type + "#456", TypedLiteralToken(tok => Assert.Equal(type, tok.Generating), IntegerLiteralToken(456)));
		}
		[Theory]
		[InlineData("INT")]
		[InlineData("UINT")]
		[InlineData("SINT")]
		[InlineData("USINT")]
		[InlineData("DINT")]
		[InlineData("UDINT")]
		[InlineData("LINT")]
		[InlineData("ULINT")]
		[InlineData("BYTE")]
		[InlineData("WORD")]
		[InlineData("DWORD")]
		[InlineData("LWORD")]
		[InlineData("REAL")]
		[InlineData("LREAL")]
		public void TypeQualifiedReal(string type)
		{
			AssertAllTokens(type + "#1.5", TypedLiteralToken(tok => Assert.Equal(type, tok.Generating), RealLiteralToken(1.5)));
		}
		[Theory]
		[InlineData("(", typeof(Compiler.ParenthesisOpenToken))]
		[InlineData(")", typeof(Compiler.ParenthesisCloseToken))]
		[InlineData("[", typeof(Compiler.BracketOpenToken))]
		[InlineData("]", typeof(Compiler.BracketCloseToken))]
		[InlineData("+", typeof(Compiler.PlusToken))]
		[InlineData("-", typeof(Compiler.MinusToken))]
		[InlineData("*", typeof(Compiler.StarToken))]
		[InlineData("**", typeof(Compiler.PowerToken))]
		[InlineData("/", typeof(Compiler.SlashToken))]
		[InlineData(",", typeof(Compiler.CommaToken))]
		[InlineData("=", typeof(Compiler.EqualToken))]
		[InlineData("=>", typeof(Compiler.DoubleArrowToken))]
		[InlineData("<=", typeof(Compiler.LessEqualToken))]
		[InlineData("<", typeof(Compiler.LessToken))]
		[InlineData("<>", typeof(Compiler.UnEqualToken))]
		[InlineData(":=", typeof(Compiler.AssignToken))]
		[InlineData(":", typeof(Compiler.ColonToken))]
		[InlineData("..", typeof(Compiler.DotsToken))]
		[InlineData(".", typeof(Compiler.DotToken))]
		[InlineData(";", typeof(Compiler.SemicolonToken))]
		[InlineData("^", typeof(Compiler.DerefToken))]
		[InlineData(">=", typeof(Compiler.GreaterEqualToken))]
		[InlineData(">", typeof(Compiler.GreaterToken))]
		[InlineData("::", typeof(Compiler.DoubleColonToken))]
		public void Symbols(string generating, Type tokenType)
		{
			AssertAllTokens(generating, tok => Assert.IsType(tokenType, tok));
		}

		[Fact]
		public void LineComment()
		{
			AssertAllTokens("// Hallo",
				EndToken.Leading(CommentToken(" Hallo").Generating("// Hallo").Position(0, 8)));
		}
		[Fact]
		public void LineCommentEndWindows()
		{
			AssertAllTokens("// Hallo\r\n123",
				IntegerLiteralToken(123).Leading(CommentToken(" Hallo\r\n")));
		}
		[Fact]
		public void LineCommentEndUnix()
		{
			AssertAllTokens("// Hallo\n123",
				IntegerLiteralToken(123).Leading(CommentToken(" Hallo\n")));
		}
		[Fact]
		public void LineCommentEndDarwin()
		{
			AssertAllTokens("// Hallo\r123",
				IntegerLiteralToken(123).Leading(CommentToken(" Hallo\r")));
		}
		[Fact]
		public void LineCommentEndMidWindows()
		{
			AssertAllTokens("// Hallo\r",
				EndToken.Leading(CommentToken(" Hallo\r")));
		}

		[Fact]
		public void MultipleLinesCommentEndUnix()
		{
			AssertAllTokens("// Hallo\n// Welt\n123",
				IntegerLiteralToken(123).Leading(CommentToken(" Welt\n").Leading(CommentToken(" Hallo\n"))));
		}
		[Fact]
		public void MultiLineComment_StartParen()
		{
			AssertAllTokens("(*Hallo\nWelt*)",
				EndToken.Leading(CommentToken("Hallo\nWelt")));
		}
		[Fact]
		public void MultiLineComment_StartSlash()
		{
			AssertAllTokens("/*Hallo\nWelt*/",
				EndToken.Leading(CommentToken("Hallo\nWelt")));
		}
		[Fact]
		public void MultiLineComment_StartSlash_InnerParenBlock()
		{
			AssertAllTokens("/*Hallo*)\nWelt*/",
				EndToken.Leading(CommentToken("Hallo*)\nWelt")));
		}
		[Fact]
		public void MultiLineComment_StartSlash_MissingEnd()
		{
			AssertAllTokens_WithError("/*Hallo*)\nWelt",
				ExactlyMessages(ErrorOfType<Compiler.Messages.MissingEndOfMultilineCommentMessage>()),
				EndToken.Leading(CommentToken("Hallo*)\nWelt")));
		}
		[Fact]
		public void MultiLineComment_StartSlash_MissingEnd_WithStartingStar()
		{
			AssertAllTokens_WithError("/*Hallo*)\nWelt*",
				ExactlyMessages(ErrorOfType<Compiler.Messages.MissingEndOfMultilineCommentMessage>()),
				EndToken.Leading(CommentToken("Hallo*)\nWelt*")));
		}
		[Theory]
		[InlineData("5")]
		[InlineData("ident")]
		[InlineData("IDENT")]
		[InlineData("_")]
		[InlineData("*")]
		[InlineData(":")]
		[InlineData("<")]
		[InlineData("::")]
		public void UnknownToken_AnyEnd(string end)
		{
			AssertAllTokens("$|" + end,
				 UnknownToken("$|"),
				 tok => Assert.Equal(end, tok.Generating));
		}
		[Fact]
		public void UnknownToken_Whitespace()
		{
			AssertAllTokens("$| ",
				 UnknownToken("$|"));
		}
		[Fact]
		public void Error_BadBooleanLiteral()
		{
			AssertAllTokens_WithError("BOOL#IF",
				ExactlyMessages(ErrorOfType<InvalidBooleanLiteralMessage>()),
				TypedLiteralToken(type => { }, token => { }),
				IfToken);
		}
		[Fact]
		public void Error_BadBooleanLiteral_EndOfFile()
		{
			AssertAllTokens_WithError("BOOL#",
				ExactlyMessages(ErrorOfType<InvalidBooleanLiteralMessage>()),
				TypedLiteralToken(type => { }, token => { }));
		}
	}
}
