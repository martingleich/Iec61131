using StandardLibraryExtensions;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace SourceGenerator
{
	public class SyntaxDescription : IBasicSyntaxElementType
	{
		[XmlElement("Name")]
		public string Name { get; set; }
		[XmlElement("Interface")]
		public List<string> InterfaceNames { get; set; }
		[XmlElement("Element")]
		public List<SyntaxElement> Elements { get; set; }

		[XmlIgnore]
		private List<SyntaxInterface> Interfaces;

		public void Initialize(Objects objects, List<string> errors)
		{
			Interfaces = new List<SyntaxInterface>();
			foreach (var itf in InterfaceNames)
			{
				if (objects.GetSyntaxInterfaceByName(itf) is SyntaxInterface realItf)
				{
					Interfaces.Add(realItf);
				}
				else
				{
					errors.Add(itf);
				}
			}
			foreach (var elem in Elements)
				elem.Initialize(objects, errors);
		}
		public bool Implements(SyntaxInterface itf)
			=> Interfaces.Any(implItf => implItf==itf || implItf.Implements(itf));
		public IEnumerable<SyntaxInterface> AllInterfaces()
		{
			HashSet<SyntaxInterface> found = new HashSet<SyntaxInterface>();
			Stack<SyntaxInterface> toVisit = new Stack<SyntaxInterface>(Interfaces);
			while (toVisit.Count > 0)
			{
				var itf = toVisit.Pop();
				if (found.Add(itf))
				{
					foreach (var n in itf.Extends)
						toVisit.Push(n);
				}
			}
			return found;
		}

		public string ArgName => LanguageUtils.GetValidInstanceIdentifier(Name);

		public string GetValueName(SyntaxElement elem)
			=> string.IsNullOrEmpty(elem.BaseName) ? "Value" : elem.BaseName;
		public Code ToCode()
		{
			var cw = new CodeWriter();
			cw.WriteLine("[ExcludeFromCodeCoverage]");
			cw.WriteLine($"public sealed partial class {Name} : {InterfaceNames.DefaultIfEmpty("ISyntax").DelimitWith(", ")}");
			cw.StartBlock();
			cw.WriteLine($"public {Name}({Elements.Select(elem => $"{elem.TypeName} {elem.ArgName}").DelimitWith(", ")})");
			cw.StartBlock();
			cw.WriteLines(Elements.Select(elem => $"{elem.Name} = {elem.ArgName};"));
			cw.WriteLine("SourcePosition = SourcePosition.ConvexHull(FirstNonNullChild.SourcePosition, LastNonNullChild.SourcePosition);");
			cw.EndBlock();
			cw.WriteLines(Elements.Select(elem => $"public readonly {elem.TypeName} {elem.Name};"));
			cw.WriteLines(Elements.Where(syn => syn.IsTokenWithValue && !syn.IsNullable).Select(elem => $"public {((TokenDescriptionWithValue)elem.BaseType).ValueType} {GetValueName(elem)} => {elem.Name}.Value;"));
			cw.WriteLines(Elements.Where(syn => syn.IsTokenWithValue && syn.IsNullable).Select(elem => $"public {((TokenDescriptionWithValue)elem.BaseType).ValueType}? {GetValueName(elem)} => {elem.Name}?.Value;"));
			foreach(var itf in AllInterfaces().Select(itf => itf.Name))
			{
				cw.WriteLine($"T {itf}.Accept<T>({itf}.IVisitor<T> visitor) => visitor.Visit(this);");
				cw.WriteLine($"void {itf}.Accept({itf}.IVisitor visitor) => visitor.Visit(this);");
			}
			cw.WriteLine($"public INode FirstNonNullChild => {FirstNonNullChild()};");
			cw.WriteLine($"public INode LastNonNullChild => {LastNonNullChild()};");
			cw.WriteLine("public SourcePosition SourcePosition {get;}");
			cw.EndBlock();
			return cw.ToCode();
		}
		private string ValueOrNullable(SyntaxElement element) => (element.IsNullable ? "(INode?)" : "") + element.Name;
		private string FirstNonNullCode(IEnumerable<SyntaxElement> syntaxElements) =>
			syntaxElements.TakeWhileIncludingLast(f => f.IsNullable).Select(ValueOrNullable).DelimitWith(" ?? ");
		private string FirstNonNullChild() => FirstNonNullCode(Elements);
		private string LastNonNullChild() => FirstNonNullCode(Enumerable.Reverse(Elements));

		private string GetTestType(SyntaxElement synElem) => $"System.Action<{synElem.TypeName}>";
		private string GetFixedTestArg(SyntaxElement synElem)
		{
			if (synElem.IsTokenInterface)
				return null;
			if (synElem.IsTokenWithValue)
				return $"{synElem.TypeName}({synElem.ArgName})";
			if (synElem.IsTokenWithoutValue && synElem.Kind == ElementKind.Normal)
				return synElem.TypeName;
			return synElem.ArgName;
		}
		private string GetFixedTestType(SyntaxElement synElem)
		{
			if (synElem.IsTokenWithValue)
				return ((TokenDescriptionWithValue)synElem.BaseType).ValueType;
			if (synElem.IsTokenWithoutValue && !synElem.IsNullable)
				return null;
			return GetTestType(synElem);
		}
		private string GetFixedTestArgDeclaration(SyntaxElement synElem)
		{
			var fixedTestType = GetFixedTestType(synElem);
			return fixedTestType != null ? $"{fixedTestType} {synElem.ArgName}" : null;
		}
		public Code ToTestCode()
		{
			var cw = new CodeWriter();
			var defaultArgs = Elements
				.Select(elem => $"{GetTestType(elem)} {elem.ArgName}")
				.DelimitWith(", ");
			cw.WriteLine($"public static SyntaxTest {Name}({defaultArgs}) => syn =>");
			cw.StartBlock();
			cw.WriteLine($"var syn{Name} = Assert.IsType<{Name}>(syn);");
			cw.WriteLines(Elements.Select(elem => $"{elem.ArgName}(syn{Name}.{elem.Name});"));
			cw.EndBlock(";");
			var betterArgs = Elements
				.Select(GetFixedTestArgDeclaration)
				.WhereNotNull()
				.DelimitWith(", ");
			if (betterArgs != defaultArgs)
			{
				var betterBaseArgs = Elements
					.Select(GetFixedTestArg)
					.WhereNotNull()
					.DelimitWith(", ");
				cw.WriteLine($"public static SyntaxTest {Name}({betterArgs}) => {Name}({betterBaseArgs});");
			}
			return cw.ToCode();
		}

		public override string ToString() => Name;
	}
}
