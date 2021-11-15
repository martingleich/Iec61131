using System.Collections.Immutable;

namespace Compiler
{
	public sealed class Scanner
	{
		private readonly StringPool StringPool = new ();
		private readonly string Text;
		private readonly LiteralScannerT LiteralScanner;
		private readonly Messages.MessageBag Messages;

		private int Cursor;

		internal Scanner(string text, Messages.MessageBag messages)
		{
			Text = text ?? throw new System.ArgumentNullException(nameof(text));
			LiteralScanner = new LiteralScannerT(this);
			Messages = messages ?? throw new System.ArgumentNullException(nameof(messages));
		}

		private IToken? SkipNonSyntax(IToken? leadingToken)
		{
			int skipStart;
			do
			{
				skipStart = Cursor;
				leadingToken = SkipWhitespace(leadingToken);
				leadingToken = SkipSingleLineComment(leadingToken);
				leadingToken = SkipMultiLineComment(leadingToken);
			} while (skipStart != Cursor);
			return leadingToken;
		}
		private IToken? SkipWhitespace(IToken? leadingToken)
		{
			int whitespaceStart = Cursor;
			while (Cursor < Text.Length && char.IsWhiteSpace(Text[Cursor]))
				++Cursor;
			if (whitespaceStart != Cursor)
			{
				var whitespace = StringPool.GetString(Text, whitespaceStart, Cursor - whitespaceStart);
				return new WhitespaceToken(whitespace, whitespace, whitespaceStart, leadingToken);
			}
			return leadingToken;
		}
		private IToken? SkipMultiLineComment(IToken? leadingToken)
		{
			if (Cursor < Text.Length - 1 && (Text[Cursor] == '(' || Text[Cursor] == '/') && Text[Cursor + 1] == '*')
			{
				var terminator = Text[Cursor] == '(' ? ')' : '/';
				int commentStart = Cursor;
				Cursor += 2;
				while (Cursor < Text.Length)
				{
					if (Text[Cursor] == '*')
					{
						++Cursor;
						if (Cursor < Text.Length && Text[Cursor] == terminator)
						{
							++Cursor;
							// End of comment
							var comment = Text[commentStart..Cursor];
							return new CommentToken(comment[2..^2], comment, commentStart, leadingToken);
						}
					}
					else
					{
						++Cursor;
					}
				}
				Messages.Add(new Messages.MissingEndOfMultilineCommentMessage(SourcePosition.FromStartLength(commentStart, 0), "*" + terminator));
				var comment2 = Text[commentStart..Cursor];
				return new CommentToken(comment2[2..], comment2, commentStart, leadingToken);
			}
			return leadingToken;
		}
		private IToken? SkipSingleLineComment(IToken? leadingToken)
		{
			if (Cursor < Text.Length - 1&& Text[Cursor] == '/' && Text[Cursor + 1] == '/')
			{
				int lineBreakCount = 0;
				int commentStart = Cursor;
				Cursor += 2;
				while (Cursor < Text.Length)
				{
					if (Text[Cursor] == '\n')
					{
						lineBreakCount = 1;
						++Cursor;
						break;
					}
					else if (Text[Cursor] == '\r')
					{
						int start = Cursor;
						++Cursor;
						if (Cursor < Text.Length && Text[Cursor] == '\n')
							++Cursor;
						lineBreakCount = Cursor-start;
						break;
					}
					else
					{
						++Cursor;
					}
				}
				var comment = Text[commentStart..Cursor];
				return new CommentToken(comment[2..], comment, commentStart, leadingToken);
			}
			return leadingToken;
		}

		private static bool IsDigit(char c) => c >= '0' && c <= '9';
		private static bool IsStartIdentifier(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
		private static bool IsMidIdentifier(char c) => IsStartIdentifier(c) || IsDigit(c) || c == '_';
		private static bool IsSymbol(char c) => c == '_' || c == '^' || c == '*' || c == ';' || c == '.' || c == ':' || c == '>' || c == '<' || c == '=' || c == '(' || c == ')' || c == '{' || c == '}' || c == '+' || c == '-' || c == '*' || c == '/' || c == ',';
		private static bool IsUnknown(char c) => !(char.IsWhiteSpace(c) || IsStartIdentifier(c) || IsSymbol(c) || IsDigit(c));

		private sealed class LiteralScannerT : IBuiltInTypeToken.IVisitor<IToken>
		{
			private readonly Scanner Scanner;

			public LiteralScannerT(Scanner scanner)
			{
				Scanner = scanner ?? throw new System.ArgumentNullException(nameof(scanner));
			}

			private IToken ScanNumber(IBuiltInTypeToken builtInType)
			{
				var literalToken = Scanner.ScanNumber(builtInType.LeadingNonSyntax);
				return new TypedLiteralToken(
					new TypedLiteral(builtInType, literalToken),
					Scanner.Text[builtInType.StartPosition..Scanner.Cursor],
					builtInType.StartPosition, builtInType.LeadingNonSyntax);
			}

			public IToken Visit(CharToken charToken)
			{
				throw new System.NotImplementedException();
			}

			public IToken Visit(LRealToken lRealToken) => ScanNumber(lRealToken);
			public IToken Visit(RealToken realToken) => ScanNumber(realToken);
			public IToken Visit(LIntToken lIntToken) => ScanNumber(lIntToken);
			public IToken Visit(DIntToken dIntToken) => ScanNumber(dIntToken);
			public IToken Visit(IntToken intToken) => ScanNumber(intToken);
			public IToken Visit(SIntToken sIntToken) => ScanNumber(sIntToken);
			public IToken Visit(ULIntToken uLIntToken) => ScanNumber(uLIntToken);
			public IToken Visit(UDIntToken uDIntToken) => ScanNumber(uDIntToken);
			public IToken Visit(UIntToken uIntToken) => ScanNumber(uIntToken);
			public IToken Visit(USIntToken uSIntToken) => ScanNumber(uSIntToken);
			public IToken Visit(LWordToken lWordToken) => ScanNumber(lWordToken);
			public IToken Visit(DWordToken dWordToken) => ScanNumber(dWordToken);
			public IToken Visit(WordToken wordToken) => ScanNumber(wordToken);
			public IToken Visit(ByteToken byteToken) => ScanNumber(byteToken);

			public IToken Visit(BoolToken boolToken)
			{
				var literalToken = Scanner.ScanBoolean(boolToken.LeadingNonSyntax);
				return new TypedLiteralToken(
					new TypedLiteral(boolToken, literalToken),
					Scanner.Text[boolToken.StartPosition..Scanner.Cursor],
					boolToken.StartPosition, boolToken.LeadingNonSyntax);
			}

			public IToken Visit(LTimeToken lTimeToken)
			{
				throw new System.NotImplementedException();
			}

			public IToken Visit(TimeToken timeToken)
			{
				throw new System.NotImplementedException();
			}

			public IToken Visit(LDTToken lDTToken)
			{
				throw new System.NotImplementedException();
			}

			public IToken Visit(DTToken dTToken)
			{
				throw new System.NotImplementedException();
			}

			public IToken Visit(LDateToken lDateToken)
			{
				throw new System.NotImplementedException();
			}

			public IToken Visit(DateToken dateToken)
			{
				throw new System.NotImplementedException();
			}

			public IToken Visit(LTODToken lTODToken)
			{
				throw new System.NotImplementedException();
			}

			public IToken Visit(TODToken tODToken)
			{
				throw new System.NotImplementedException();
			}
		}

		private ILiteralToken ScanNumber(IToken? leadingToken)
		{
			int start = Cursor;
			int valueStart;
			bool isNegative;
			if (Cursor < Text.Length && Text[Cursor] == '-')
			{
				isNegative = true;
				valueStart = start + 1;
				++Cursor;
			}
			else if (Cursor < Text.Length && Text[Cursor] == '+')
			{
				isNegative = false;
				valueStart = start + 1;
				++Cursor;
			}
			else
			{
				isNegative = false;
				valueStart = start;
			}
			while (Cursor < Text.Length && (IsDigit(Text[Cursor]) || Text[Cursor] == '_'))
				++Cursor;
			if (Cursor < Text.Length && Text[Cursor] == '.' && Cursor < Text.Length - 1 && (Text[Cursor] == '_' || IsDigit(Text[Cursor + 1])))
			{
				++Cursor;
				while (Cursor < Text.Length && IsDigit(Text[Cursor]))
					++Cursor;
				var generating = Text[start..Cursor];
				var literalValue = new RealLiteralToken(OverflowingReal.Parse(generating), generating, start, leadingToken);
					return literalValue;
			}
			else
			{
				var generating = Text[start..Cursor];
				var pureValue = Text[valueStart..Cursor];
				var value = OverflowingInteger.Parse(pureValue, isNegative);
				return new IntegerLiteralToken(value, generating, start, leadingToken);
			}
		}

		private ILiteralToken ScanBoolean(IToken? leadingToken)
		{
			if (TryScanKeyword(TrueToken.DefaultGenerating, leadingToken) is IToken trueToken)
				return (ILiteralToken)trueToken;
			else if (TryScanKeyword(FalseToken.DefaultGenerating, leadingToken) is IToken falseToken)
				return (ILiteralToken)falseToken;
			else if (Cursor < Text.Length && IsDigit(Text[Cursor]))
				return ScanNumber(leadingToken);
			else
			{
				Messages.Add(new Messages.InvalidBooleanLiteralMessage(SourcePosition.FromStartLength(Cursor, 0)));
				return new FalseToken("", Cursor, leadingToken);
			}
		}

		private IToken? TryScanKeyword(string keyword, IToken? leadingToken)
		{
			if (Cursor < Text.Length - keyword.Length + 1)
			{
				var generating = Text[Cursor..(Cursor + keyword.Length)];
				if (generating.ToCaseInsensitive() == keyword.ToCaseInsensitive())
				{
					if (ScannerKeywordTable.TryMap(generating, Cursor, leadingToken) is IToken token)
					{
						Cursor += keyword.Length;
						return token;
					}
				}
			}
			return null;
		}

		private IToken ScanIdentifier(IToken? leadingToken)
		{
			int start = Cursor - 1;
			while (Cursor < Text.Length && IsMidIdentifier(Text[Cursor]))
				++Cursor;
			var generating = StringPool.GetString(Text, start, Cursor-start);
			if (ScannerKeywordTable.TryMap(generating, start, leadingToken) is IToken token)
			{
				if (token is IBuiltInTypeToken builtInTypeToken)
				{
					if (Cursor < Text.Length && Text[Cursor] == '#')
					{
						++Cursor;
						// Type qualified literal => Parse the literal depending on the builtInType
						return builtInTypeToken.Accept(LiteralScanner);
					}
				}
				return token;
			}
			else
				return new IdentifierToken(generating.ToCaseInsensitive(), generating, start, leadingToken);
		}

		private IToken ScanUnknown(IToken? leadingToken)
		{
			int start = Cursor - 1;
			while (Cursor < Text.Length && IsUnknown(Text[Cursor]))
				++Cursor;
			var generating = Text[start..Cursor];
			return new UnknownToken(generating, generating, start, leadingToken);
		}

		private IToken NextInternal(IToken? skipped)
		{
			var leadingToken = SkipNonSyntax(skipped);
			if (Cursor >= Text.Length)
				return new EndToken(Cursor, leadingToken);

			int start = Cursor;
			++Cursor;
			char cur = Text[start];
			if (IsDigit(cur))
			{
				--Cursor;
				return ScanNumber(leadingToken);
			}
			else if (IsStartIdentifier(cur))
				return ScanIdentifier(leadingToken);
			else if (cur == '[')
				return new BracketOpenToken(start, leadingToken);
			else if (cur == ']')
				return new BracketCloseToken(start, leadingToken);
			else if (cur == '(')
				return new ParenthesisOpenToken(start, leadingToken);
			else if (cur == ')')
				return new ParenthesisCloseToken(start, leadingToken);
			else if (cur == '{')
			{
				while (Cursor < Text.Length)
				{
					var c = Text[Cursor];
					++Cursor;
					if (c == '}')
					{
						var generating = Text[start..Cursor];
						var value = generating[1..^1];
						return new AttributeToken(value, generating, start, leadingToken);
					}
				}
				Messages.Add(new Messages.MissingEndOfAttributeMessage(SourcePosition.FromStartLength(start, 0)));
				var generating2 = Text[start..Cursor];
				var value2 = generating2[1..];
				return new AttributeToken(value2, generating2, start, leadingToken);
			}
			else if (cur == '+')
				return new PlusToken(start, leadingToken);
			else if (cur == '-')
				return new MinusToken(start, leadingToken);
			else if (cur == '*')
			{
				if (Cursor < Text.Length && Text[Cursor] == '*')
				{
					++Cursor;
					return new PowerToken(start, leadingToken);
				}
				else
				{
					return new StarToken(start, leadingToken);
				}
			}
			else if (cur == '/')
				return new SlashToken(start, leadingToken);
			else if (cur == ',')
				return new CommaToken(start, leadingToken);
			else if (cur == '=')
			{
				if (Cursor < Text.Length && Text[Cursor] == '>')
				{
					++Cursor;
					return new DoubleArrowToken(start, leadingToken);
				}
				else
				{
					return new EqualToken(start, leadingToken);
				}
			}
			else if (cur == '<')
			{
				if (Cursor < Text.Length && Text[Cursor] == '=')
				{
					++Cursor;
					return new LessEqualToken(start, leadingToken);
				}
				else if (Cursor < Text.Length && Text[Cursor] == '>')
				{
					++Cursor;
					return new UnEqualToken(start, leadingToken);
				}
				else
				{
					return new LessToken(start, leadingToken);
				}
			}
			else if (cur == '>')
			{
				if (Cursor < Text.Length && Text[Cursor] == '=')
				{
					++Cursor;
					return new GreaterEqualToken(start, leadingToken);
				}
				else
				{
					return new GreaterToken(start, leadingToken);
				}
			}
			else if (cur == ':')
			{
				if (Cursor < Text.Length && Text[Cursor] == '=')
				{
					++Cursor;
					return new AssignToken(start, leadingToken);
				}
				else
				{
					return new ColonToken(start, leadingToken);
				}
			}
			else if (cur == '.')
			{
				if (Cursor < Text.Length && Text[Cursor] == '.')
				{
					++Cursor;
					return new DotsToken(start, leadingToken);
				}
				else
				{
					return new DotToken(start, leadingToken);
				}
			}
			else if (cur == ';')
				return new SemicolonToken(start, leadingToken);
			else if (cur == '^')
				return new DerefToken(start, leadingToken);
			else
				return ScanUnknown(leadingToken);
		}

		private IToken? _last;
		private IToken FixTrailing(IToken next)
		{
			var lastNonSyntax = next.LeadingNonSyntax;
			IToken? trailing = null;
			while (lastNonSyntax != null)
			{
				lastNonSyntax.TrailingNonSyntax = trailing;
				trailing = lastNonSyntax;
				lastNonSyntax = lastNonSyntax.TrailingNonSyntax;
			}
			if (_last != null)
				_last.TrailingNonSyntax = trailing;
			return _last = next;
		}
		public IToken Next() => Next(null);
		public IToken Next(IToken? skipped)
		{
			var next = NextInternal(skipped);
			return FixTrailing(next);
		}

		public static ImmutableArray<IToken> Tokenize(string input, Messages.MessageBag messages)
		{
			var allTokens = ImmutableArray.CreateBuilder<IToken>();
			var scanner = new Scanner(input, messages);
			IToken token;
			do
			{
				token = scanner.Next(null);
				allTokens.Add(token);
			} while (!(token is EndToken));
			return allTokens.ToImmutable();
		}

		public override string ToString() => Text.Insert(Cursor, "^");
	}
}
