using System;
using System.Text;

namespace Compiler
{
	public static class SyntaxToStringConverter
	{
		public static string ExactToString(ISyntax syntax)
		{
			var sb = new StringBuilder();
			ExactToString(syntax, sb);
			return sb.ToString();
		}
		private static void ExactToString(ISyntax syntax, StringBuilder sb)
		{
			foreach (var child in syntax.GetChildren())
			{
				if (child is ISyntax childSyntax)
					ExactToString(childSyntax, sb);
				else if (child is IToken tokenSyntax)
					ExactToString(tokenSyntax, sb);
				else
					throw new ArgumentException($"Cannot convert the type '{syntax.GetType()}' to a string.");
			}
		}
		private static void ExactToString(IToken token, StringBuilder sb)
		{
			if (token.LeadingNonSyntax != null)
				ExactToString(token.LeadingNonSyntax, sb);
			sb.Append(token.Generating);
		}
	}
}
