using System.Xml.Serialization;
namespace SourceGenerator
{
	public enum ElementKind
	{
		[XmlEnum("Normal")]
		Normal,
		[XmlEnum("Nullable")]
		Nullable,
		[XmlEnum("Array")]
		Array,
		[XmlEnum("CommaSeparated")]
		CommaSeparated,
	}
}