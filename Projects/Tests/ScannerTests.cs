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
	}
}
