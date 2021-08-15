using Compiler;
using System;
using Xunit;

using TokenTest = System.Action<Compiler.IToken>;
using MessageTest = System.Action<System.Collections.Generic.IEnumerable<Compiler.Messages.IMessage>>;
using Compiler.Messages;

namespace Tests
{
	public static partial class ScannerTestHelper
	{
		public static TokenTest AnyToken(Type t) => tok => Assert.IsType(t, tok);
		public static TokenTest IntegerLiteralToken(ulong expected) => IntegerLiteralToken(OverflowingInteger.FromUlong(expected, false));
		public static TokenTest IntegerLiteralToken(long expected) => IntegerLiteralToken(OverflowingInteger.FromUlong((ulong)Math.Abs(expected), expected < 0));
		public static TokenTest RealLiteralToken(double expected) => RealLiteralToken(OverflowingReal.FromDouble(expected));
		public static TokenTest TypedLiteralToken(TokenTest type, TokenTest value) => tok =>
		{
			var typedLiteral = Assert.IsType<TypedLiteralToken>(tok);
			type(typedLiteral.Value.Type);
			value(typedLiteral.Value.LiteralToken);
		};
		public static TokenTest Generating(this TokenTest self, string generating) => tok =>
		{
			self(tok);
			Assert.Equal(generating, tok.Generating);
		};
		public static TokenTest Position(this TokenTest self, int pos, int length) => tok =>
		{
			self(tok);
			Assert.Equal(pos, tok.StartPosition);
			Assert.Equal(length, tok.Length);
			Assert.Equal(SourcePosition.FromStartLength(pos, length), tok.SourcePosition);
		};
		public static TokenTest Leading(this TokenTest self, TokenTest leader) => tok =>
		{
			leader(tok.LeadingNonSyntax);
			self(tok);
		};
		public static TokenTest Trailing(this TokenTest self, TokenTest trailer) => tok =>
		{
			self(tok);
			trailer(tok.TrailingNonSyntax);
		};

		public static void AssertAllTokens(string input, params TokenTest[] tests)
			=> AssertAllTokens_WithError(input, Assert.Empty, tests);
		public static void AssertAllTokens_WithError(string input, MessageTest messageTest, params TokenTest[] tests)
		{
			int i = 0;
			var msgBag = new MessageBag();
			var allTokens = Scanner.Tokenize(input, msgBag);
			foreach (var test in tests)
			{
				test(allTokens[i]);
				++i;
			}
			// Automatically test for end if no explicit test.
			if (i < allTokens.Length)
				EndToken(allTokens[i]);

			messageTest(msgBag);
		}
	}
}
