using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace SourceGenerator
{
	public class TokenClasses
	{
		[XmlElement("TokenWithValueClass")]
		public List<TokenDescriptionWithValue> TokenWithValueClasses { get; set; }
		[XmlElement("TokenKeywordClass")]
		public List<TokenDescriptionKeyword> TokenKeywordClasses { get; set; }
		[XmlElement("TokenWithoutValueClass")]
		public List<TokenDescriptionWithoutValue> TokenWithoutValueClasses { get; set; }

		public IEnumerable<TokenDescription> All => TokenWithoutValueClasses.Concat<TokenDescription>(TokenKeywordClasses).Concat(TokenWithValueClasses);
	}
}
