﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Compiler
{
	public sealed partial class Parser
	{
		private readonly Stack<IToken> BackupStack = new();
		private IToken CurToken;
		private readonly Scanner Scanner;
		private readonly Messages.MessageBag Messages;

		private static readonly CommaSeperatedParser<RangeSyntax, BracketCloseToken> CommaSeperatedRangeParser =
			MakeCommaSeperatedParser(p => p.ParseRange(), IsExpressionStartToken, BracketCloseToken.Synthesize);
		private static readonly CommaSeperatedParser<IExpressionSyntax, BracketCloseToken> CommaSeperatedIndexParser =
			MakeCommaSeperatedParser(p => p.ParseExpression(), IsExpressionStartToken, BracketCloseToken.Synthesize);
		private static readonly CommaSeperatedParser<CallArgumentSyntax, ParenthesisCloseToken> CommaSeperatedCallArgumentParser =
			MakeCommaSeperatedParser(p => p.ParseCallArgument(), IsExpressionStartToken, ParenthesisCloseToken.Synthesize);
		private static readonly CommaSeperatedParser<EnumValueDeclarationSyntax, ParenthesisCloseToken> CommaSeperatedEnumValueDeclarationParser =
			MakeCommaSeperatedParser(p => p.ParseEnumValueDeclaration(), t => t is IdentifierToken, ParenthesisCloseToken.Synthesize);
		private static readonly CommaSeperatedParser<IInitializerElementSyntax, BraceCloseToken> CommaSeperatedInitializerElementParser =
			MakeCommaSeperatedParser(p => p.ParseInitializerElement(), IsInitializerElementStartToken, BraceCloseToken.Synthesize);

		private Parser(string file, string text, Messages.MessageBag messages)
		{
			Scanner = new Scanner(file, text, messages);
			CurToken = Scanner.Next(null);
			Messages = messages ?? throw new ArgumentNullException(nameof(messages));

		}
		CallArgumentSyntax ParseCallArgument()
		{
			var explicitParameter = TryParseExplicitCallParameterSyntax();
			var value = ParseExpression();
			return new CallArgumentSyntax(explicitParameter, value);

			ExplicitCallParameterSyntax? TryParseExplicitCallParameterSyntax()
			{
				if (TryMatch<IdentifierToken>(out var tokenIdentifier))
				{
					if (TryMatch<IParameterKindToken>(out var tokenParameterKind))
					{
						return new(tokenIdentifier, tokenParameterKind);
					}
					else
					{
						PutBack(tokenIdentifier);
					}
				}
				return null;
			}
		}
		EnumValueDeclarationSyntax ParseEnumValueDeclaration()
		{
			var tokenIdentifier = Match(IdentifierToken.Synthesize);
			var value = TryParseVarInit();
			return new(tokenIdentifier, value);
		}
		IInitializerElementSyntax ParseInitializerElement()
		{
			if (TryParseElement() is IElementSyntax element)
			{
				var tokenAssign = Match(AssignToken.Synthesize);
				var value = ParseExpression();
				return new ExplicitInitializerElementSyntax(element, tokenAssign, value);
			}
			else
			{
				var value = ParseExpression();
				return new ImplicitInitializerElementSyntax(value);
			}
			IElementSyntax? TryParseElement()
			{
				if (TryMatch<DotToken>(out var tokenDot))
				{
					var tokenName = Match(IdentifierToken.Synthesize);
					return new FieldElementSyntax(tokenDot, tokenName);
				}
				else if (TryMatch<BracketOpenToken>(out var tokenBracketOpen))
				{
					if (TryMatch<DotsToken>(out var tokenDots))
					{
						var tokenBracketClose = Match(BracketCloseToken.Synthesize);
						return new AllIndicesElementSyntax(tokenBracketOpen, tokenDots, tokenBracketClose);
					}
					else
					{
						var index = ParseExpression();
						var tokenBracketClose = Match(BracketCloseToken.Synthesize);
						return new IndexElementSyntax(tokenBracketOpen, index, tokenBracketClose);
					}
				}
				else
				{
					return null;
				}
			}
		}

		static bool IsInitializerElementStartToken(IToken token)
			=> IsExpressionStartToken(token) || token is DotToken || token is BracketOpenToken;
		static bool IsExpressionStartToken(IToken token) =>
			token is IBuiltInTypeToken ||
			token is SizeOfToken ||
			token is AdrToken ||
			token is ILiteralToken ||
			token is IdentifierToken ||
			token is ParenthesisOpenToken ||
			token is IUnaryOperatorToken ||
			token is IdentifierToken ||
			token is BraceOpenToken;

		public static (PouInterfaceSyntax Interface, StatementListSyntax Body) ParsePou(string file, string input, Messages.MessageBag messages)
		{
			var parser = new Parser(file, input, messages);
			var result = parser.ParsePou();
			parser.ExpectEnd();
			return result;
		}

		public static PouInterfaceSyntax ParsePouInterface(string file, string input, Messages.MessageBag messages)
		{
			var parser = new Parser(file, input, messages);
			var result = parser.ParsePouInterface(forceComplete: true);
			parser.ExpectEnd();
			return result;
		}
		public static StatementListSyntax ParsePouBody(string file, string input, Messages.MessageBag messages)
		{
			var parser = new Parser(file, input, messages);
			var result = parser.ParsePouBody();
			parser.ExpectEnd();
			return result;
		}
		public static ITypeSyntax ParseType(string file, string input, Messages.MessageBag messages)
		{
			var parser = new Parser(file, input, messages);
			var result = parser.ParseType();
			parser.ExpectEnd();
			return result;
		}
		public static IExpressionSyntax ParseExpression(string file, string input, Messages.MessageBag messages)
		{
			var parser = new Parser(file, input, messages);
			var result = parser.ParseExpression();
			parser.ExpectEnd();
			return result;
		}
		public static TypeDeclarationSyntax ParseTypeDeclaration(string file, string input, Messages.MessageBag messages)
		{
			var parser = new Parser(file, input, messages);
			var result = parser.ParseTypeDeclaration();
			parser.ExpectEnd();
			return result;
		}
		public static GlobalVarListSyntax ParseGlobalVarList(string file, string input, Messages.MessageBag messages)
		{
			var parser = new Parser(file, input, messages);
			var result = parser.ParseGlobalVarListSyntax();
			parser.ExpectEnd();
			return result;
		}

		private GlobalVarListSyntax ParseGlobalVarListSyntax()
		{
			var attributes = ParseAttributes();
			var varDeclBlocks = ParseVariableDeclBlocks(forceComplete: true);
			return new GlobalVarListSyntax(attributes, varDeclBlocks);
		}

		private StatementListSyntax ParsePouBody() => ParseStatementList(EndToken.Synthesize, out _);

		private StatementListSyntax ParseStatementList<TEnd>(Func<SourcePoint, TEnd> endSynthesiszer, out TEnd outEnd) where TEnd : class, IToken
		{
			var defaultStart = CurToken.SourceSpan;
			var list = ImmutableArray.CreateBuilder<IStatementSyntax>();
			while (CurToken is not TEnd && CurToken is not EndToken)
			{
				var statement = ParseStatement();
				list.Add(statement);
			}
			outEnd = Match(endSynthesiszer);
			return new StatementListSyntax(list.ToSyntaxArray(defaultStart));
		}
		private IStatementSyntax ParseStatement()
		{
			if (TryMatch<IfToken>(out var tokenIf))
			{
				return ParseIfStatement(tokenIf);
			}
			else if (TryMatch<WhileToken>(out var tokenWhile))
			{
				return ParseWhileLoop(tokenWhile);
			}
			else if (TryMatch<ForToken>(out var tokenFor))
			{
				return ParseForStatement(tokenFor);
			}
			else if (TryMatch<ReturnToken>(out var tokenReturn))
			{
				return new ReturnStatementSyntax(tokenReturn, Match(SemicolonToken.Synthesize));
			}
			else if (TryMatch<ExitToken>(out var tokenExit))
			{
				return new ExitStatementSyntax(tokenExit, Match(SemicolonToken.Synthesize));
			}
			else if (TryMatch<ContinueToken>(out var tokenContinue))
			{
				return new ContinueStatementSyntax(tokenContinue, Match(SemicolonToken.Synthesize));
			}
			else if (TryMatch<SemicolonToken>(out var tokenSemicolon))
			{
				return new EmptyStatementSyntax(tokenSemicolon);
			}
			else
			{
				var expr = ParseExpression();
				if (TryMatch<SemicolonToken>(out var tokenSemicolon2))
				{
					return new ExpressionStatementSyntax(expr, tokenSemicolon2);
				}
				else
				{
					var tokenAssign = Match(AssignToken.Synthesize);
					var rightExpr = ParseExpression();
					var tokenSemicolon3 = Match(SemicolonToken.Synthesize);
					return new AssignStatementSyntax(expr, tokenAssign, rightExpr, tokenSemicolon3);
				}
			}

			ForStatementSyntax ParseForStatement(ForToken tokenFor)
			{
				var index = ParseExpression();
				var tokenAssign = Match(AssignToken.Synthesize);
				var initial = ParseExpression();
				var tokenTo = Match(ToToken.Synthesize);
				var upperBound = ParseExpression();
				var byClause = TryParseForByClause();
				var tokenDo = Match(DoToken.Synthesize);
				var statements = ParseStatementList(EndForToken.Synthesize, out var tokenEndFor);
				return new ForStatementSyntax(tokenFor, index, tokenAssign, initial, tokenTo, upperBound, byClause, tokenDo, statements, tokenEndFor);

				ForByClauseSyntax? TryParseForByClause()
				{
					if (TryMatch<ByToken>(out var tokenBy))
					{
						var stepSize = ParseExpression();
						return new ForByClauseSyntax(tokenBy, stepSize);
					}
					else
					{
						return null;
					}
				}
			}

			WhileStatementSyntax ParseWhileLoop(WhileToken tokenWhile)
			{
				var condition = ParseExpression();
				var tokenDo = Match(DoToken.Synthesize);
				var statements = ParseStatementList(EndWhileToken.Synthesize, out var tokenEndWhile);
				return new WhileStatementSyntax(tokenWhile, condition, tokenDo, statements, tokenEndWhile);
			}

			IfStatementSyntax ParseIfStatement(IfToken tokenIf)
			{
				var statements = ImmutableArray.CreateBuilder<IStatementSyntax>();
				var elsifBranches = ImmutableArray.CreateBuilder<ElsifBranchSyntax>();

				var condition = ParseExpression();
				var tokenThen = Match(ThenToken.Synthesize);
				while (CurToken is not EndToken && CurToken is not ElsifToken && CurToken is not ElseToken && CurToken is not EndIfToken)
					statements.Add(ParseStatement());
				var ifBranch = new IfBranchSyntax(tokenIf, condition, tokenThen, statements.ToStatementList(tokenThen.SourceSpan));
				statements.Clear();
				while (TryMatch<ElsifToken>(out var tokenElsif))
				{
					condition = ParseExpression();
					tokenThen = Match(ThenToken.Synthesize);
					while (CurToken is not EndToken && CurToken is not ElsifToken && CurToken is not ElseToken && CurToken is not EndIfToken)
						statements.Add(ParseStatement());
					elsifBranches.Add(new ElsifBranchSyntax(tokenElsif, condition, tokenThen, statements.ToStatementList(tokenThen.SourceSpan)));
					statements.Clear();
				}
				ElseBranchSyntax? elseBranch;
				if (TryMatch<ElseToken>(out var tokenElse))
				{
					while (CurToken is not EndToken && CurToken is not EndIfToken)
						statements.Add(ParseStatement());
					elseBranch = new ElseBranchSyntax(tokenElse, statements.ToStatementList(tokenElse.SourceSpan));
				}
				else
				{
					elseBranch = null;
				}

				var tokenEndIf = Match(EndIfToken.Synthesize);

				return new IfStatementSyntax(ifBranch, elsifBranches.ToSyntaxArray(tokenThen.SourceSpan), elseBranch, tokenEndIf);
			}
		}

		private SyntaxArray<AttributeSyntax> ParseAttributes()
		{
			var defaultPosition = CurToken.SourceSpan;
			var attributes = ImmutableArray.CreateBuilder<AttributeSyntax>();
			while (TryMatch<AttributeToken>(out var tokenAttribute))
				attributes.Add(new AttributeSyntax(tokenAttribute));
			return attributes.ToSyntaxArray(defaultPosition);
		}
		private (PouInterfaceSyntax Interface, StatementListSyntax Body) ParsePou()
		{
			var @interface = ParsePouInterface(forceComplete: false);
			var body = ParsePouBody();
			return (@interface, body);
		}

		private PouInterfaceSyntax ParsePouInterface(bool forceComplete)
		{
			var attributes = ParseAttributes();
			var tokenPouKind = Match<IPouKindToken>(FunctionToken.Synthesize);
			var tokenName = Match(IdentifierToken.Synthesize);
			var returnDeclaration = TryParseReturnDeclaration();
			var variableDeclBlocks = ParseVariableDeclBlocks(forceComplete);
			return new PouInterfaceSyntax(attributes, tokenPouKind, tokenName, returnDeclaration, variableDeclBlocks);

			ReturnDeclSyntax? TryParseReturnDeclaration()
			{
				if (TryMatch<ColonToken>(out var tokenColon))
				{
					var type = ParseType();
					return new ReturnDeclSyntax(tokenColon, type);
				}
				else
				{
					return null;
				}
			}
		}

		private SyntaxArray<VarDeclBlockSyntax> ParseVariableDeclBlocks(bool forceComplete)
		{
			var defaultStart = CurToken.SourceSpan;
			var list = ImmutableArray.CreateBuilder<VarDeclBlockSyntax>();
			while (CurToken is not EndToken)
			{
				if (TryMatch<IVarDeclKindToken>(out var tokenVarDeclKind))
				{
					var varDeclBlock = ParseVariableDeclBlock(tokenVarDeclKind);
					list.Add(varDeclBlock);
				}
				else
				{
					if (forceComplete)
					{
						AddUnexpectedTokenMessage(typeof(VarToken), typeof(VarInputToken), typeof(VarOutToken), typeof(VarInOutToken), typeof(VarTempToken));
						SkipUntil<IVarDeclKindToken>();
					}
					else
					{
						break;
					}
				}
			}
			return list.ToSyntaxArray(defaultStart);

			VarDeclBlockSyntax ParseVariableDeclBlock(IVarDeclKindToken tokenVarDeclKind)
			{
				TryMatch<ConstantToken>(out var tokenConstant);
				var declarations = ParseVariableDeclarations(EndVarToken.Synthesize, out var tokenEndVar);
				return new(tokenVarDeclKind, tokenConstant, declarations, tokenEndVar);
			}
		}
		private SyntaxArray<VarDeclSyntax> ParseVariableDeclarations<TEnd>(Func<SourcePoint, TEnd> synthesizeEnd, out TEnd tokenEndVar) where TEnd : class, IToken
		{
			var defaultStart = CurToken.SourceSpan;
			var list = ImmutableArray.CreateBuilder<VarDeclSyntax>();
			while (CurToken is not EndToken && CurToken is not TEnd)
			{
				var vardecl = ParseVariableDeclaration();
				list.Add(vardecl);
			}
			tokenEndVar = Match(synthesizeEnd);
			return list.ToSyntaxArray(defaultStart);

			VarDeclSyntax ParseVariableDeclaration()
			{
				var attributes = ParseAttributes();
				var tokenIdentifier = Match(IdentifierToken.Synthesize);
				var tokenColon = Match(ColonToken.Synthesize);
				var type = ParseType();
				var initial = TryParseVarInit();
				var tokenSemicolon = Match(SemicolonToken.Synthesize);
				return new(attributes, tokenIdentifier, tokenColon, type, initial, tokenSemicolon);
			}
		}

		VarInitSyntax? TryParseVarInit()
		{
			if (TryMatch<AssignToken>(out var tokenAssign))
			{
				var value = ParseExpression();
				return new(tokenAssign, value);
			}
			else
			{
				return null;
			}
		}

		private ITypeSyntax? TryParseBuiltInType()
		{
			if (TryMatch<IBuiltInTypeToken>(out var tokenBuiltInType))
			{
				var type = new BuiltInTypeSyntax(tokenBuiltInType);
				if (TryMatch<ParenthesisOpenToken>(out var tokenParenOpen))
				{
					var range = ParseRange();
					var tokenParenClose = Match(ParenthesisCloseToken.Synthesize);
					return new SubrangeTypeSyntax(type, tokenParenOpen, range, tokenParenClose);
				}
				return type;
			}
			else if (TryMatch<ArrayToken>(out var tokenArray))
			{
				var tokenBracketOpen = Match(BracketOpenToken.Synthesize);
				var ranges = CommaSeperatedRangeParser.Parse(this, out var tokenBracketClose);
				var tokenOf = Match(OfToken.Synthesize);
				var baseType = ParseType();
				return new ArrayTypeSyntax(tokenArray, tokenBracketOpen, ranges, tokenBracketClose, tokenOf, baseType);
			}
			else if (TryMatch<PointerToken>(out var tokenPointer))
			{
				var toToken = Match(ToToken.Synthesize);
				var baseType = ParseType();
				return new PointerTypeSyntax(tokenPointer, toToken, baseType);
			}
			else if (TryMatch<StringToken>(out var tokenString))
			{
				var size = TryParseStringSize();
				return new StringTypeSyntax(tokenString, size);
			}
			else
			{
				return null;
			}
			StringSizeSyntax? TryParseStringSize()
			{
				if (TryMatch<BracketOpenToken>(out var tokenBracketOpen))
				{
					var size = ParseExpression();
					var tokenBracketClose = Match(BracketCloseToken.Synthesize);
					return new StringSizeSyntax(tokenBracketOpen, size, tokenBracketClose);
				}
				else
				{
					return null;
				}
			}
		}
		private ITypeSyntax ParseType()
		{
			if (TryParseBuiltInType() is ITypeSyntax builtInType)
			{
				return builtInType;
			}
			else if (TryMatch<IdentifierToken>(out var tokenIdentifier))
			{
				var scope = TryParseScopeQualifier(ref tokenIdentifier);
				if (scope != null)
					return new ScopedIdentifierTypeSyntax(scope, tokenIdentifier);
				else
					return new IdentifierTypeSyntax(tokenIdentifier);
			}
			else
			{
				Messages.Add(new Messages.TypeExpectedMessage(CurToken));
				//SkipUntil(resyncTokens);
				return new BuiltInTypeSyntax(Synthesize(IntToken.Synthesize));
			}
		}
		private RangeSyntax ParseRange()
		{
			var lowerBound = ParseExpression();
			var tokenDots = Match(DotsToken.Synthesize);
			var upperBound = ParseExpression();
			return new RangeSyntax(lowerBound, tokenDots, upperBound);
		}
		private IExpressionSyntax ParseExpression()
		{
			return ParseOrExpression();

			IExpressionSyntax ParseOrExpression()
			{
				var value = ParseXorExpression();
				while (TryMatch<OrToken>(out var orToken))
					value = new BinaryOperatorExpressionSyntax(value, orToken, ParseXorExpression());
				return value;
			}
			IExpressionSyntax ParseXorExpression()
			{
				var value = ParseAndExpression();
				while (TryMatch<XorToken>(out var xorToken))
					value = new BinaryOperatorExpressionSyntax(value, xorToken, ParseAndExpression());
				return value;
			}
			IExpressionSyntax ParseAndExpression()
			{
				var value = ParseCompareExpression();
				while (TryMatch<AndToken>(out var andToken))
					return new BinaryOperatorExpressionSyntax(value, andToken, ParseCompareExpression());
				return value;
			}
			IExpressionSyntax ParseCompareExpression()
			{
				var value = ParseEquExpression();
				while (TryMatch<ICompareBinaryOperatorToken>(out var compareToken))
					value = new BinaryOperatorExpressionSyntax(value, compareToken, ParseEquExpression());
				return value;
			}
			IExpressionSyntax ParseEquExpression()
			{
				var value = ParseAddExpression();
				while (TryMatch<IEquBinaryOperatorToken>(out var equToken))
					value = new BinaryOperatorExpressionSyntax(value, equToken, ParseAddExpression());
				return value;
			}
			IExpressionSyntax ParseAddExpression()
			{
				var value = ParseTermExpression();
				while (TryMatch<IAddBinaryOperatorToken>(out var addToken))
					value = new BinaryOperatorExpressionSyntax(value, addToken, ParseTermExpression());
				return value;
			}
			IExpressionSyntax ParseTermExpression()
			{
				var value = ParsePowerExpression();
				while (TryMatch<ITermBinaryOperatorToken>(out var termToken))
					value = new BinaryOperatorExpressionSyntax(value, termToken, ParsePowerExpression());
				return value;
			}
			IExpressionSyntax ParsePowerExpression()
			{
				var value = ParseUnaryExpression();
				while (TryMatch<PowerToken>(out var powerToken))
					value = new BinaryOperatorExpressionSyntax(value, powerToken, ParseUnaryExpression());
				return value;
			}

			IExpressionSyntax ParseUnaryExpression()
			{
				if (TryMatch<IUnaryOperatorToken>(out var unaryToken))
					return new UnaryOperatorExpressionSyntax(unaryToken, ParsePostfixOperator());
				else
					return ParsePostfixOperator();
			}

			IExpressionSyntax ParsePostfixOperator()
			{
				var expression = ParsePrimaryExpression();
				return ParsePostfixOperatorInternal(expression);
			}

			IExpressionSyntax ParsePostfixOperatorInternal(IExpressionSyntax leftExpression)
			{
				if (TryMatch<BracketOpenToken>(out var tokenBracketOpen))
				{
					var indices = CommaSeperatedIndexParser.Parse(this, out var tokenBracketClose);
					var expr = new IndexAccessExpressionSyntax(leftExpression, tokenBracketOpen, indices, tokenBracketClose);
					return ParsePostfixOperatorInternal(expr);
				}
				else if (TryMatch<DerefToken>(out var tokenDeref))
				{
					var expr = new DerefExpressionSyntax(leftExpression, tokenDeref);
					return ParsePostfixOperatorInternal(expr);
				}
				else if (TryMatch<ParenthesisOpenToken>(out var tokenParenOpen))
				{
					var arguments = CommaSeperatedCallArgumentParser.Parse(this, out var tokenParenClose);
					var expr = new CallExpressionSyntax(leftExpression, tokenParenOpen, arguments, tokenParenClose);
					return ParsePostfixOperatorInternal(expr);
				}
				else if (TryMatch<DotToken>(out var tokenDot))
				{
					var tokenIdentifier = Match(IdentifierToken.Synthesize);
					var expr = new CompoAccessExpressionSyntax(leftExpression, tokenDot, tokenIdentifier);
					return ParsePostfixOperatorInternal(expr);
				}
				else
				{
					return leftExpression;
				}
			}

			IExpressionSyntax ParseTypedInitializationExpressionSyntax(ITypeSyntax type, HashToken tokenHash)
			{
				var tokenBraceOpen = Match(BraceOpenToken.Synthesize);
				var elements = CommaSeperatedInitializerElementParser.Parse(this, out var tokenBraceClose);
				var initializer = new InitializationExpressionSyntax(tokenBraceOpen, elements, tokenBraceClose);
				return new TypedInitializationExpressionSyntax(type, tokenHash, initializer);
			}
			IExpressionSyntax ParsePrimaryExpression()
			{
				if (TryMatch<ILiteralToken>(out var tokenLiteral))
				{
					return new LiteralExpressionSyntax(tokenLiteral);
				}
				else if (TryParseBuiltInType() is ITypeSyntax builtInType)
				{
					var tokenHash = Match(HashToken.Synthesize);
					return ParseTypedInitializationExpressionSyntax(builtInType, tokenHash);
				}
				else if (TryMatch<IdentifierToken>(out var tokenIdentifier))
				{
					var scope = TryParseScopeQualifier(ref tokenIdentifier);
					if (TryMatch<HashToken>(out var tokenHash))
					{
						ITypeSyntax type = scope != null
							? new ScopedIdentifierTypeSyntax(scope, tokenIdentifier)
							: new IdentifierTypeSyntax(tokenIdentifier);
						return ParseTypedInitializationExpressionSyntax(type, tokenHash);
					}
					else
					{
						if (scope != null)
							return new ScopedVariableExpressionSyntax(scope, tokenIdentifier);
						else
							return new VariableExpressionSyntax(tokenIdentifier);
					}
				}
				else if (TryMatch<ParenthesisOpenToken>(out var tokenParenOpen))
				{
					var innerExpression = ParseExpression();
					var tokenParenClose = Match(ParenthesisCloseToken.Synthesize);
					return new ParenthesisedExpressionSyntax(tokenParenOpen, innerExpression, tokenParenClose);
				}
				else if (TryMatch<SizeOfToken>(out var tokenSizeOf))
				{
					var tokenParenOpen2 = Match(ParenthesisOpenToken.Synthesize);
					ITypeSyntax arg = ParseType();
					var tokenParenClose = Match(ParenthesisCloseToken.Synthesize);
					return new SizeOfExpressionSyntax(tokenSizeOf, tokenParenOpen2, arg, tokenParenClose);
				}
				else if (TryMatch<BraceOpenToken>(out var tokenBraceOpen))
				{
					var elements = CommaSeperatedInitializerElementParser.Parse(this, out var tokenBraceClose);
					return new InitializationExpressionSyntax(tokenBraceOpen, elements, tokenBraceClose);
				}
				else
				{
					Messages.Add(new Messages.ExpectedExpressionMessage(CurToken.SourceSpan));
					Skip(); // Skip the bad token.
					return new VariableExpressionSyntax(Synthesize(IdentifierToken.Synthesize));
				}
			}
		}

		private ScopeQualifierSyntax? TryParseScopeQualifier(ref IdentifierToken tokenIdentifier)
		{
			ScopeQualifierSyntax? scope = null;
			while (TryMatch<DoubleColonToken>(out var tokenDoubleColon))
			{
				scope = new ScopeQualifierSyntax(scope, tokenIdentifier, tokenDoubleColon);
				tokenIdentifier = Match(IdentifierToken.Synthesize);
			}
			return scope;
		}

		private TypeDeclarationSyntax ParseTypeDeclaration()
		{
			var attributes = ParseAttributes();
			var tokenType = Match(TypeToken.Synthesize);
			var tokenIdentifier = Match(IdentifierToken.Synthesize);
			var tokenColon = Match(ColonToken.Synthesize);
			var typeBody = ParseTypeBody();
			var tokenSemicolon = Match(SemicolonToken.Synthesize);
			var tokenEndType = Match(EndTypeToken.Synthesize);
			return new(attributes, tokenType, tokenIdentifier, tokenColon, typeBody, tokenSemicolon, tokenEndType);

			ITypeDeclarationBodySyntax ParseTypeBody()
			{
				if (TryMatch<StructToken>(out var tokenStruct))
				{
					return ParseStruct(tokenStruct);
				}
				else if (TryMatch<UnionToken>(out var tokenUnion))
				{
					return ParseUnion(tokenUnion);
				}
				else if (TryMatch<ParenthesisOpenToken>(out var tokenParenthesisOpen))
				{
					return ParseEnum(null, tokenParenthesisOpen);
				}
				else
				{
					var baseType = ParseType();
					if (TryMatch<ParenthesisOpenToken>(out var tokenParenthesisOpen2))
						return ParseEnum(baseType, tokenParenthesisOpen2);
					else
						return ParseAlias(baseType);
				}

				StructTypeDeclarationBodySyntax ParseStruct(StructToken tokenStruct)
				{
					var fields = ParseVariableDeclarations(EndStructToken.Synthesize, out var tokenEndStruct);
					var initial = TryParseVarInit();
					return new(tokenStruct, fields, tokenEndStruct, initial);
				}
				UnionTypeDeclarationBodySyntax ParseUnion(UnionToken tokenUnion)
				{
					var fields = ParseVariableDeclarations(EndUnionToken.Synthesize, out var tokenEndUnion);
					var initial = TryParseVarInit();
					return new(tokenUnion, fields, tokenEndUnion, initial);
				}

				EnumTypeDeclarationBodySyntax ParseEnum(ITypeSyntax? baseType, ParenthesisOpenToken tokenParenthesisOpen)
				{
					var values = CommaSeperatedEnumValueDeclarationParser.Parse(this, out var tokenParenthesisClose);
					var initial = TryParseVarInit();
					return new(baseType, tokenParenthesisOpen, values, tokenParenthesisClose, initial);
				}

				AliasTypeDeclarationBodySyntax ParseAlias(ITypeSyntax baseType)
				{
					var initial = TryParseVarInit();
					return new(baseType, initial);
				}
			}
		}
	}

	public sealed partial class Parser
	{
		private class CommaSeperatedParser<TElement, TEnd> where TElement : ISyntax where TEnd : class, IToken
		{
			private readonly Func<Parser, TElement> ParseElement;
			private readonly Func<IToken, bool> IsValid;
			private readonly Func<SourcePoint, TEnd> MakeEnd;

			public CommaSeperatedParser(Func<Parser, TElement> parseElement, Func<IToken, bool> isValid, Func<SourcePoint, TEnd> makeEnd)
			{
				ParseElement = parseElement ?? throw new ArgumentNullException(nameof(parseElement));
				IsValid = isValid ?? throw new ArgumentNullException(nameof(isValid));
				MakeEnd = makeEnd ?? throw new ArgumentNullException(nameof(makeEnd));
			}

			public SyntaxCommaSeparated<TElement> Parse(Parser parser, out TEnd endToken)
			{
				var start = parser.CurToken.SourceSpan.End;
				var firstParameter = ParseHead(parser, out endToken);
				return new SyntaxCommaSeparated<TElement>(firstParameter, SourceSpan.FromStartLength(start, 0));
			}

			private SyntaxCommaSeparated<TElement>.HeadSyntax? ParseHead(Parser parser, out TEnd TEnd)
			{
				if (parser.TryMatch<TEnd>(out var maybeEndToken))
				{
					TEnd = maybeEndToken;
					return null;
				}
				else if (IsValid(parser.CurToken))
				{
					var element = ParseElement(parser);
					var tail = TryParseTail(parser, out TEnd);
					return new SyntaxCommaSeparated<TElement>.HeadSyntax(element, tail);
				}
				else
				{
					TEnd = parser.Match(MakeEnd);
					return null;
				}
			}

			private SyntaxCommaSeparated<TElement>.TailSyntax? TryParseTail(Parser parser, out TEnd end)
			{
				if (parser.TryMatch<CommaToken>(out var commaToken))
				{
					var element = ParseElement(parser);
					var tail = TryParseTail(parser, out end);
					return new SyntaxCommaSeparated<TElement>.TailSyntax(commaToken, element, tail);
				}
				else if (parser.TryMatch<TEnd>(out var x))
				{
					end = x;
					return null;
				}
				else
				{
					parser.AddUnexpectedTokenMessage(typeof(CommaToken), typeof(TEnd));
					end = MakeEnd(parser.CurToken.StartPosition);
					return null;
				}
			}
		}

		private static CommaSeperatedParser<TElement, TEnd> MakeCommaSeperatedParser<TElement, TEnd>(Func<Parser, TElement> parseElement, Func<IToken, bool> isValid, Func<SourcePoint, TEnd> makeEnd)
			where TElement : ISyntax where TEnd : class, IToken
		{
			return new CommaSeperatedParser<TElement, TEnd>(parseElement, isValid, makeEnd);
		}

		private void AddUnexpectedTokenMessage(params Type[] expected)
		{
			Messages.Add(new Messages.UnexpectedTokenMessage(CurToken, expected));
		}

		private void PutBack(IToken backed)
		{
			BackupStack.Push(CurToken);
			CurToken = backed;
		}

		private void Skip()
		{
			if (BackupStack.Count > 0)
				CurToken = BackupStack.Pop();
			else
				CurToken = Scanner.Next(CurToken);
		}
		private void SkipUntil<T>() where T : IToken
		{
			while (CurToken is not T && CurToken is not EndToken)
				Skip();
		}
		private bool TryMatch<T>([NotNullWhen(true)] out T? result) where T : class, IToken
		{
			result = CurToken as T;
			if (result != null)
			{
				if (BackupStack.Count > 0)
					CurToken = BackupStack.Pop();
				else
					CurToken = Scanner.Next(null);
			}
			return result != null;
		}
		private T Match<T>(Func<SourcePoint, T> synthesizer) where T : class, IToken
		{
			if (!TryMatch<T>(out var result))
				result = SynthesizeWithError(synthesizer);
			return result;
		}
		private T Synthesize<T>(Func<SourcePoint, T> synthesizer) => synthesizer(CurToken.StartPosition);
		private T SynthesizeWithError<T>(Func<SourcePoint, T> synthesizer)
		{
			AddUnexpectedTokenMessage(typeof(T));
			return Synthesize(synthesizer);
		}
		private void ExpectEnd()
		{
			Match(EndToken.Synthesize);
		}
	}
}

