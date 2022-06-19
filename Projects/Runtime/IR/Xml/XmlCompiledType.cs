using Runtime.IR.RuntimeTypes;
using Superpower;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Runtime.IR.Xml
{
    public sealed class XmlCompiledType
    {
        [XmlType("property")]
        public sealed class XmlProperty
        {
            [XmlAttribute("name")]
            [System.Diagnostics.CodeAnalysis.AllowNull]
            public string Name;
            [XmlAttribute("type")]
            [System.Diagnostics.CodeAnalysis.AllowNull]
            public string Type;
            [XmlAttribute("offset")]
            public int Offset;
            public static XmlProperty FromValue(RuntimeTypeStructured.Property property) => new()
            {
                Name = property.Name,
                Type = property.Type.Name,
                Offset = property.Offset
            };
            public RuntimeTypeStructured.Property ToValue(Func<string, IRuntimeType> typeParser) => new(
                    Name,
                    typeParser(Type),
                    Offset);
        }

        [XmlAttribute("name")]
        [System.Diagnostics.CodeAnalysis.AllowNull]
        public string Name;
        [XmlAttribute("size")]
        public int Size;
        [XmlArray("properties")]
        [System.Diagnostics.CodeAnalysis.AllowNull]
        public List<XmlProperty> Properties;
        public static XmlCompiledType Load(Stream stream)
        {
            return (XmlCompiledType)_serializer.Deserialize(stream)!;
        }
        public static void ToXml(RuntimeTypeStructured type, Stream dst)
        {
            using var writer = XmlWriter.Create(dst, new()
            {
                Encoding = Encoding.UTF8,
                Indent = true,
            });
            var xml = FromType(type);
            _serializer.Serialize(writer, xml);
        }
        public static XmlCompiledType FromType(RuntimeTypeStructured compiled)
        {
            return new()
            {
                Name = compiled.Name,
                Size = compiled.Size,
                Properties = compiled.Properties.Values.Select(XmlProperty.FromValue).ToList(),
            };
        }

        public RuntimeTypeStructured ToType(Func<string, IRuntimeType> parser)
        {
            return new(
                Name,
                Size,
                Properties.Select(v => v.ToValue(parser)).ToImmutableArray());
        }
        private static readonly XmlSerializer _serializer = new(typeof(XmlCompiledType));

        public static ImmutableArray<RuntimeTypeStructured> ConvertTypes(IEnumerable<XmlCompiledType> typeTable)
        {
            var ctx = new RuntimeTypeParser(typeTable.ToImmutableDictionary(x => x.Name));
            return typeTable.Select(ctx.Convert).ToImmutableArray();
        }
    }
}
