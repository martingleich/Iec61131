using Compiler;
using Compiler.Messages;
using System;
using Xunit;

namespace Tests
{
	public static partial class ParserTestHelper
	{
		public static Func<string, T> NoErrorParse<T>(Func<string, MessageBag, T> parse) => input =>
		{
			var parseMessages = new MessageBag();
			var source = parse(input, parseMessages);
			Assert.Empty(parseMessages);
			return source;
		};
		public static TypeDeclarationSyntax ParseTypeDeclaration(string input) => NoErrorParse(Parser.ParseTypeDeclaration)(input);
		public static ITypeSyntax ParseType(string input) => NoErrorParse(Parser.ParseType)(input);
		public static IStatementSyntax ParseStatements(string input) => NoErrorParse(Parser.ParsePouBody)(input);
		public static IExpressionSyntax ParseExpression(string input) => NoErrorParse(Parser.ParseExpression)(input);
		public static Action<SyntaxArray<T>> SyntaxArray<T>(params Action<T>[] checkes) where T : ISyntax => arr =>
		{
			Assert.Collection(arr.Values, checkes);
		};
		public static Action<ISyntax> VariableExpressionSyntax(string name) =>
			VariableExpressionSyntax(name.ToCaseInsensitive());
	}
}
