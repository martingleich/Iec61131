using StandardLibraryExtensions;
using System.Linq;
using System.Xml.Serialization;

namespace SourceGenerator
{
	public class TokenDescriptionWithValue : TokenDescription
	{
		[XmlElement("ValueType")]
		public string ValueType { get; set; }
		[XmlElement("DefaultValue")]
		public string DefaultValue { get; set; }

		public override Code ToCode()
		{
			var cw = new CodeWriter();
			cw.WriteLine("[ExcludeFromCodeCoverage]");
			cw.WriteLine($"public sealed partial class {Name} : {Interfaces.Prepend($"DefaultTokenWithValueImplementation<{ValueType}>").DelimitWith(", ")}");
			cw.StartBlock();
			cw.WriteLine($"public {Name}({ValueType} value, string? generating, SourcePoint startPosition, IToken? leadingNonSyntax) : base(value, generating, startPosition, leadingNonSyntax) {{ }}");
			if(DefaultValue != null)
				cw.WriteLine($"public readonly static Func<SourcePoint, {Name}> Synthesize = startPosition => SynthesizeEx(startPosition, {DefaultValue});");
			cw.WriteLine($"public static {Name} SynthesizeEx(SourcePoint startPosition, {ValueType} value) => new {Name}(value, null, startPosition, null);");
			cw.WriteLine($"public override string ToString() => \"{Name}(\" + Value.ToString() + \")\";");
			cw.WriteCode(GetInterfaceImplCode());
			cw.EndBlock();
			return cw.ToCode();
		}

		public override Code ToTestCode()
		{
			var cw = new CodeWriter();
			cw.WriteLine($"public static TokenTest {Name}({ValueType} value) => tok => Assert.Equal(value, Assert.IsType<{Name}>(tok).Value);");
			cw.WriteLine($"public static readonly TokenTest Any{Name} = tok => Assert.IsType<{Name}>(tok);");
			return cw.ToCode();
		}

		public override string ToString() => $"{Name}({ValueType})";
	}

}
