using Compiler;
using Compiler.Messages;
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
	}
}
