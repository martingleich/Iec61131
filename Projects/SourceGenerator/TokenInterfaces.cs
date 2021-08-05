using System.Collections.Generic;
using System.Xml.Serialization;

namespace SourceGenerator
{
	public class TokenInterfaces
	{
		[XmlElement("TokenInterface")]
		public List<TokenInterface> All { get; set; }
	}
}
