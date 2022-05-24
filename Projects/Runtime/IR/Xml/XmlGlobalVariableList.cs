using Superpower;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Runtime.IR.Xml
{
    [System.Xml.Serialization.XmlType("gvl")]
    public sealed class XmlGlobalVariableList
    {
        [System.Diagnostics.CodeAnalysis.AllowNull]
        [System.Xml.Serialization.XmlAttribute("name")]
        public string Name;
        [System.Xml.Serialization.XmlAttribute("area")]
        public ushort Area;
        [System.Xml.Serialization.XmlAttribute("size")]
        public ushort Size;
        [System.Xml.Serialization.XmlElement("initializer")]
        public XmlCompiledPou? Initializer;
        [System.Xml.Serialization.XmlArray("variables")]
        public List<XmlGlobalVariable>? Variables;

        public static XmlGlobalVariableList FromObject(CompiledGlobalVariableList obj) => new()
        {
            Name = obj.Name,
            Area = obj.Area,
            Size = obj.Size,
            Initializer = obj.Initializer != null ? XmlCompiledPou.FromCompiledPou(obj.Initializer) : null,
            Variables = obj.VariableTable?.Select(XmlGlobalVariable.FromObject).ToList(),
        };

        public CompiledGlobalVariableList ToObject() => new(
            Name, Area, Size, Initializer?.ToCompiledPou(), Variables?.Select(v => v.ToObject()).ToImmutableArray());


		private static readonly System.Xml.Serialization.XmlSerializer _serializer = new (typeof(XmlGlobalVariableList));
		public static CompiledGlobalVariableList Parse(Stream stream)
        {
            var xml = (XmlGlobalVariableList)_serializer.Deserialize(stream)!;
            return xml.ToObject();
        }

		public static void ToXml(CompiledGlobalVariableList obj, Stream stream)
		{
			using var writer = XmlWriter.Create(stream, new()
			{
				Encoding = Encoding.UTF8,
				Indent = true,
			});
			var xml = FromObject(obj);
			_serializer.Serialize(writer, xml);
		}
    }
}
