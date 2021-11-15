using Compiler;
using Compiler.Messages;
using System;
using Xunit;
using System.Linq;

namespace Tests
{
	public static partial class ParserTestHelper
	{
		public static readonly Action<ISyntax> NullSyntax = input =>
		{
			Assert.Null(input);
		};
		public static Func<string, T> ParseWithError<T>(Func<string, MessageBag, T> parse, params Action<IMessage>[] checks) => input =>
		{
			var parseMessages = new MessageBag();
			var source = parse(input, parseMessages);
			ErrorTestHelper.ExactlyMessages(checks)(parseMessages);
			return source;
		};
		public static Func<string, T> NoErrorParse<T>(Func<string, MessageBag, T> parse) => ParseWithError(parse);
		public static TypeDeclarationSyntax ParseTypeDeclaration(string input) => NoErrorParse(Parser.ParseTypeDeclaration)(input);
		public static ITypeSyntax ParseType(string input) => NoErrorParse(Parser.ParseType)(input);
		public static IStatementSyntax ParseStatements(string input) => NoErrorParse(Parser.ParsePouBody)(input);
		public static IExpressionSyntax ParseExpression(string input, params Action<IMessage>[] checks) => ParseWithError(Parser.ParseExpression, checks)(input);
		public static Action<SyntaxArray<T>> SyntaxArray<T>(params Action<T>[] checkes) where T : ISyntax => arr =>
		{
			Assert.Collection(arr.Values, checkes);
		};
		public static Action<ISyntax> VariableExpressionSyntax(string name) =>
			VariableExpressionSyntax(name.ToCaseInsensitive());
	}
}
