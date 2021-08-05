using System.Collections.Generic;
using System.Xml.Serialization;

namespace SourceGenerator
{
	public abstract class TokenDescription : IBasicSyntaxElementType
	{
		[XmlElement("Name")]
		public string Name { get; set; }
		[XmlElement("Interface")]
		public List<string> Interfaces { get; set; }

		public string ArgName => LanguageUtils.GetValidInstanceIdentifier(Name);

		public abstract Code ToCode();
		public abstract Code ToTestCode();

		public Code GetInterfaceImplCode()
		{
			var cw = new CodeWriter();
			foreach (var itf in Interfaces)
				cw.WriteLine($"T {itf}.Accept<T>({itf}.IVisitor<T> visitor) => visitor.Visit(this);");
			return cw.ToCode();
		}
	}
}
