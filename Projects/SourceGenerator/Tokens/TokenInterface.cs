using System.Collections.Immutable;
using System.Xml.Serialization;

namespace SourceGenerator
{
	public class TokenInterface : IBasicSyntaxElementType
	{
		[XmlElement("Name")]
		public string Name { get; set; }

		public Code ToCode(ImmutableArray<TokenDescription> implementations)
		{
			var cw = new CodeWriter();
			cw.WriteLine($"public interface {Name} : IToken");
			cw.StartBlock();
			cw.WriteLine("public interface IVisitor<out T>");
			cw.StartBlock();
			foreach (var impl in implementations)
				cw.WriteLine($"T Visit({impl.Name} {impl.ArgName});");
			cw.EndBlock();
			cw.WriteLine("T Accept<T>(IVisitor<T> visitor);");
			cw.EndBlock();
			return cw.ToCode();
		}

		public override string ToString() => Name;
	}
}
