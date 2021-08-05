using Microsoft.CodeAnalysis.CSharp;
using StandardLibraryExtensions;
using System.Text;

namespace SourceGenerator
{
	public static class LanguageUtils
	{
		public static string GetValidInstanceIdentifier(string className)
		{
			if (className.EndsWith("?"))
				className = className.Remove(className.Length - 1, 1);
			var argName = className.ReplaceAt(0, char.ToLowerInvariant(className[0]));
			var token = SyntaxFactory.ParseToken(argName);
			if(token.IsKeyword())
				argName = $"@{argName}";
			return argName;
		}

		public static string ToCSharpString(string value)
		{
			var sb = new StringBuilder();
			foreach (var c in value)
			{
				if (c == '\n')
					sb.Append("\\n");
				else
					sb.Append(c);
			}
			return $"\"{sb}\"";
		}
	}
}
