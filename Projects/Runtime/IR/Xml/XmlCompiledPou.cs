using Superpower;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;

namespace Runtime.IR.Xml
{
    public sealed class XmlCompiledPou
    {
        [System.Xml.Serialization.XmlType("arg")]
        public sealed class XmlArg
        {
            [System.Xml.Serialization.XmlAttribute("offset")]
            public ushort Offset;
            [System.Xml.Serialization.XmlAttribute("size")]
            public int Size;

            internal static XmlArg FromTuple(CompiledArgument arg) => new()
            {
                Offset = arg.Offset.Offset,
                Size = arg.Type.Size
            };

            internal CompiledArgument ToTuple() =>
                new(new LocalVarOffset(Offset), new Type(Size));
        }

        [System.Xml.Serialization.XmlAttribute("id")]
        [System.Diagnostics.CodeAnalysis.AllowNull]
        public string Id;
        [System.Xml.Serialization.XmlArray("inputs")]
        [System.Diagnostics.CodeAnalysis.AllowNull]
        public List<XmlArg> Inputs;
        [System.Xml.Serialization.XmlArray("outputs")]
        [System.Diagnostics.CodeAnalysis.AllowNull]
        public List<XmlArg> Outputs;
        [System.Xml.Serialization.XmlElement("stackusage")]
        public int StackUsage;
        [System.Xml.Serialization.XmlElement("code")]
        [System.Diagnostics.CodeAnalysis.AllowNull]
        public XmlCode Code;

        // Debug data
        [System.Xml.Serialization.XmlElement("originalPath")]
        public string? OriginalPath;
        [System.Xml.Serialization.XmlElement("breakpoints")]
        public byte[]? Breakpoints;
        [System.Xml.Serialization.XmlArray("variables")]
        public List<XmlStackVariable>? VariableTable;

        private static byte[]? FromBreakpointsMap(BreakpointMap? breakpointMap)
        {
            if (breakpointMap == null)
                return null;
            using (var memStream = new MemoryStream())
            {
                using (var zipStream = new GZipStream(memStream, CompressionMode.Compress, true))
                {
                    breakpointMap.SerializeToStream(zipStream);
                }
                return memStream.ToArray();
            }
        }
        private static BreakpointMap? ToBreakpointsMap(byte[]? bits)
        {
            if (bits == null)
                return null;
            using (var memStream = new MemoryStream(bits))
            {
                using (var zipStream = new GZipStream(memStream, CompressionMode.Decompress, true))
                {
                    return BreakpointMap.DeserializeFromStream(zipStream);
                }
            }
        }
        public static XmlCompiledPou FromCompiledPou(CompiledPou compiled)
        {
            return new()
            {
                Id = compiled.Id.Name,
                Inputs = compiled.InputArgs.Select(XmlArg.FromTuple).ToList(),
                Outputs = compiled.OutputArgs.Select(XmlArg.FromTuple).ToList(),
                StackUsage = compiled.StackUsage,
                Code = XmlCode.FromStatements(compiled.Code),
                Breakpoints = FromBreakpointsMap(compiled.BreakpointMap),
                OriginalPath = compiled.OriginalPath,
                VariableTable = XmlVariables.FromVariableTable(compiled.VariableTable)
            };
        }

        public CompiledPou ToCompiledPou()
        {
            return new(
                new PouId(Id),
                StackUsage,
                Inputs.Select(input => input.ToTuple()).ToImmutableArray(),
                Outputs.Select(input => input.ToTuple()).ToImmutableArray(),
                Code.ToCode())
            {
                BreakpointMap = ToBreakpointsMap(Breakpoints),
                VariableTable = XmlVariables.ToTable(VariableTable),
                OriginalPath = OriginalPath
            };
        }
        private static readonly System.Xml.Serialization.XmlSerializer _serializer = new(typeof(XmlCompiledPou));
        public static CompiledPou Parse(Stream stream)
        {
            var value = (XmlCompiledPou)_serializer.Deserialize(stream)!;
            return value.ToCompiledPou();
        }
        public static void ToXml(CompiledPou pou, Stream dst)
        {
            using var writer = XmlWriter.Create(dst, new()
            {
                Encoding = Encoding.UTF8,
                Indent = true,
            });
            var xml = FromCompiledPou(pou);
            _serializer.Serialize(writer, xml);
        }
    }
}
