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
			cw.WriteLine($"public {Name}(SourcePoint startPosition, IToken? leadingNonSyntax) : this(startPosition, leadingNonSyntax, false) {{ }}");
			cw.WriteLine($"private {Name}(SourcePoint startPosition, IToken? leadingNonSyntax, bool isError) : base(startPosition, leadingNonSyntax) {{ IsError = isError;}}");
			cw.WriteLine($"public static readonly string DefaultGenerating = {LanguageUtils.ToCSharpString(Generating)};");
			cw.WriteLine($"public readonly bool IsError;");
			cw.WriteLine($"public override string? Generating => IsError ? null : DefaultGenerating;");
			cw.WriteLine($"public static readonly Func<SourcePoint, {Name}> Synthesize = startPosition => new {Name}(startPosition, null, true);");
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
