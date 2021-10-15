using Compiler;
using Compiler.Messages;
using System;
using Xunit;

namespace Tests
{
	public static partial class ParserTestHelper
	{
		public static TypeDeclarationSyntax ParseTypeDeclaration(string input)
		{
			var parseMessages = new MessageBag();
			var source = Parser.ParseTypeDeclaration(input, parseMessages);
			Assert.Empty(parseMessages);
			return source;
		}
		public static ITypeSyntax ParseType(string input)
		{
			var parseMessages = new MessageBag();
			var source = Parser.ParseType(input, parseMessages);
			Assert.Empty(parseMessages);
			return source;
		}
		public static IStatementSyntax ParseStatements(string input)
		{
			var parseMessages = new MessageBag();
			var source = Parser.ParsePouBody(input, parseMessages);
			Assert.Empty(parseMessages);
			return source;
		}
		public static Action<SyntaxArray<T>> SyntaxArray<T>(params Action<T>[] checkes) where T : ISyntax => arr =>
		{
			Assert.Collection(arr.Values, checkes);
		};
		public static Action<ISyntax> VariableExpressionSyntax(string name) =>
			VariableExpressionSyntax(name.ToCaseInsensitive());
	}
}
