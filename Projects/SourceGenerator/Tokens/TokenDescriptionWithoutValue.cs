using StandardLibraryExtensions;
using System.Linq;
using System.Xml.Serialization;

namespace SourceGenerator
{
	public class TokenDescriptionWithoutValue : TokenDescription
	{
		[XmlElement("Generating")]
		public string Generating { get; set; }

		public override Code ToCode()
		{
			var cw = new CodeWriter();
			cw.WriteLine("[ExcludeFromCodeCoverage]");
			cw.WriteLine($"public sealed partial class {Name} : {Interfaces.Prepend("DefaultTokenImplementation").DelimitWith(", ")}");
			cw.StartBlock();
			cw.WriteLine($"public {Name}(int startPosition, IToken? leadingNonSyntax) : base(startPosition, leadingNonSyntax) {{ }}");
			cw.WriteLine($"public override string Generating => {LanguageUtils.ToCSharpString(Generating)};");
			cw.WriteLine($"public static readonly Func<int, {Name}> Synthesize = startPosition => new {Name}(startPosition, null);");
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

	public class TokenDescriptionKeyword : TokenDescription
	{
		[XmlElement("Generating")]
		public string Generating { get; set; }

		public override Code ToCode()
		{
			var cw = new CodeWriter();
			cw.WriteLine("[ExcludeFromCodeCoverage]");
			cw.WriteLine($"public sealed partial class {Name} : {Interfaces.Prepend("DefaultTokenImplementation").DelimitWith(", ")}");
			cw.StartBlock();
			cw.WriteLine($"public {Name}(string generating, int startPosition, IToken? leadingNonSyntax) : base(startPosition, leadingNonSyntax)");
			cw.StartBlock();
			cw.WriteLine("Generating = generating;");
			cw.EndBlock();
			cw.WriteLine($"public override string Generating {{ get; }}");
			cw.WriteLine($"public static readonly Func<int, {Name}> Synthesize = startPosition => new {Name}({LanguageUtils.ToCSharpString(Generating)}, startPosition, null);");
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
