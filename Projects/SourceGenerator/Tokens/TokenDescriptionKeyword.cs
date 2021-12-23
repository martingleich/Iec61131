using StandardLibraryExtensions;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace SourceGenerator
{
	public class TokenDescriptionKeyword : TokenDescription
	{
		[XmlElement("Generating")]
		public string Generating { get; set; }

		public static Code WriteKeywordTable(IEnumerable<TokenDescriptionKeyword> keywords)
		{
			var cw = new CodeWriter();
			cw.WriteLine("[ExcludeFromCodeCoverage]");
			cw.WriteLine("public static class ScannerKeywordTable");
			cw.StartBlock();
			cw.WriteLine($"private static readonly System.Collections.Generic.Dictionary<string, Func<string, SourcePoint, IToken?, IToken>> Table = new(System.StringComparer.InvariantCultureIgnoreCase)");
			cw.StartBlock();
			foreach (var keyword in keywords)
				cw.WriteLine($"[{keyword.Name}.DefaultGenerating] = {keyword.Name}.Create,");
			cw.EndBlock(";");
			cw.WriteLine("public static IToken? TryMap(string potentialKeyword, SourcePoint startPosition, IToken? leadingNonSyntax)");
			cw.StartBlock();
			cw.WriteLine("if (Table.TryGetValue(potentialKeyword, out var creator))");
			cw.StartBlock();
			cw.WriteLine("return creator(potentialKeyword, startPosition, leadingNonSyntax);");
			cw.EndBlock();
			cw.WriteLine("return null;");
			cw.EndBlock();
			cw.EndBlock();
			return cw.ToCode();
		}
		public override Code ToCode()
		{
			var cw = new CodeWriter();
			cw.WriteLine("[ExcludeFromCodeCoverage]");
			cw.WriteLine($"public sealed partial class {Name} : {Interfaces.Prepend("DefaultTokenImplementation").DelimitWith(", ")}");
			cw.StartBlock();
			cw.WriteLine($"public {Name}(string generating, SourcePoint startPosition, IToken? leadingNonSyntax) : base(startPosition, leadingNonSyntax)");
			cw.StartBlock();
			cw.WriteLine("Generating = generating;");
			cw.EndBlock();
			cw.WriteLine($"public override string Generating {{ get; }}");
			cw.WriteLine($"public static readonly string DefaultGenerating = {LanguageUtils.ToCSharpString(Generating)};");
			cw.WriteLine($"public static readonly Func<SourcePoint, {Name}> Synthesize = startPosition => new {Name}(DefaultGenerating, startPosition, null);");
			cw.WriteLine($"public static readonly Func<string, SourcePoint, IToken?, {Name}> Create = (generating, startPosition, leadingNonSyntax) => new {Name}(generating, startPosition, leadingNonSyntax);");
			cw.WriteLine($"public override string ToString() => nameof({Name});");
			cw.WriteCode(GetInterfaceImplCode());
			cw.EndBlock();
			return cw.ToCode();
		}

		public override Code ToTestCode()
		{
			var cw = new CodeWriter();
			cw.WriteLine($"public static readonly TokenTest {Name} = tok => Assert.IsType<{Name}>(tok);");
			return cw.ToCode();
		}

		public override string ToString() => Name;
	}
}
