using System.Xml.Serialization;

namespace SourceGenerator
{
	[XmlRoot(ElementName = "Configuration")]
	public class Configuration
	{
		[XmlElement("GenerateTestCode")]
		public bool GenerateTestCode { get; set; }
		[XmlIgnore]
		public string ObjectsFilePath { get; set; }
	}
}
