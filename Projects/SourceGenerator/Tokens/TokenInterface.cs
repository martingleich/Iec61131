using StandardLibraryExtensions;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Xml.Serialization;

namespace SourceGenerator
{
	public class TokenInterface : IBasicSyntaxElementType
	{
		[XmlElement("Name")]
		public string Name { get; set; }
		[XmlElement("BaseInterface")]
		public List<string> BaseInterfaces { get; set; }

		public Code ToCode(ImmutableArray<TokenDescription> implementations)
		{
			var cw = new CodeWriter();
			var interfaces = BaseInterfaces.Count == 0 ? new List<string>() { "IToken" } : BaseInterfaces;
			var overwrite = BaseInterfaces.Count == 0 ? "" : "new ";
			cw.WriteLine($"public interface {Name} : {interfaces.DelimitWith(", ")}");
			cw.StartBlock();

			cw.WriteLine($"public {overwrite}interface IVisitor<out T>");
			cw.StartBlock();
			foreach (var impl in implementations)
				cw.WriteLine($"T Visit({impl.Name} {impl.ArgName});");
			cw.EndBlock();
			cw.WriteLine($"public {overwrite}interface IVisitor<out T, TContext>");
			cw.StartBlock();
			foreach (var impl in implementations)
				cw.WriteLine($"T Visit({impl.Name} {impl.ArgName}, TContext context);");
			cw.EndBlock();
			cw.WriteLine("T Accept<T>(IVisitor<T> visitor);");
			cw.WriteLine("T Accept<T, TContext>(IVisitor<T, TContext> visitor, TContext context);");

			cw.EndBlock();
			return cw.ToCode();
		}

		public override string ToString() => Name;
	}
}
