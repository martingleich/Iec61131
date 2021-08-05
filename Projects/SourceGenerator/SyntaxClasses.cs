using System.Collections.Generic;
using System.Xml.Serialization;

namespace SourceGenerator
{
	public class SyntaxClasses
	{
		[XmlElement("SyntaxClass")]
		public List<SyntaxDescription> All { get; set; }
	}
}
