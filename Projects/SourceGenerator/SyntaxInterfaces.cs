using System.Collections.Generic;
using System.Xml.Serialization;

namespace SourceGenerator
{
	public class SyntaxInterfaces
	{
		[XmlElement("SyntaxInterface")]
		public List<SyntaxInterface> All { get; set; }
	}
}
