using StandardLibraryExtensions;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Serialization;

namespace SourceGenerator
{
	public class SyntaxInterface : IBasicSyntaxElementType
	{
		[XmlElement("Name")]
		public string Name { get; set; }
		[XmlElement("Extends")]
		public List<string> ExtendsNames { get; set; }

		public IReadOnlyList<SyntaxInterface> Extends { get => extends; }
		[XmlIgnore]
		private List<SyntaxInterface> extends;

		public void Initialize(Objects objects, List<string> errors)
		{
			extends = new List<SyntaxInterface>();
			foreach (var itf in ExtendsNames)
			{
				if (objects.GetSyntaxInterfaceByName(itf) is SyntaxInterface realItf)
				{
					extends.Add(realItf);
				}
				else
				{
					errors.Add(itf);
				}
			}
		}

		public bool Implements(SyntaxInterface itf)
		{
			return Extends.Any(exItf => exItf == itf || exItf.Implements(itf));
		}

		public Code ToCode(ImmutableArray<SyntaxDescription> implementations)
		{
			var cw = new CodeWriter();
			cw.WriteLine($"public interface {Name} : {ExtendsNames.DefaultIfEmpty("ISyntax").DelimitWith(", ")}");
			cw.StartBlock();
			string newQualifier = ExtendsNames.Any() ? "new" : "";
			cw.WriteLine($"public {newQualifier} interface IVisitor<out T>");
			cw.StartBlock();
			foreach (var impl in implementations)
				cw.WriteLine($"T Visit({impl.Name} {impl.ArgName});");
			cw.EndBlock();
			cw.WriteLine("T Accept<T>(IVisitor<T> visitor);");

			cw.WriteLine($"public {newQualifier} interface IVisitor<out T, TContext>");
			cw.StartBlock();
			foreach (var impl in implementations)
				cw.WriteLine($"T Visit({impl.Name} {impl.ArgName}, TContext context);");
			cw.EndBlock();
			cw.WriteLine("T Accept<T, TContext>(IVisitor<T, TContext> visitor, TContext context);");

			cw.WriteLine($"public {newQualifier} interface IVisitor");
			cw.StartBlock();
			foreach (var impl in implementations)
				cw.WriteLine($"void Visit({impl.Name} {impl.ArgName});");
			cw.EndBlock();
			cw.WriteLine("void Accept(IVisitor visitor);");
			cw.EndBlock();

			return cw.ToCode();
		}

		public override string ToString() => Name;
	}
}
