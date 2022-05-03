using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using StandardLibraryExtensions;
using Superpower;

namespace Runtime.IR
{
    using global::Runtime.IR.Xml;
    using Statements;
	using System.IO;
	using System.IO.Compression;
    using System.Text;
    using System.Xml;

    public static class Parser
	{
		private static ImmutableArray<IStatement> FixLabels(ImmutableArray<IStatement> statements)
		{
			Dictionary<string, int> offsets = new Dictionary<string, int>();
			int id = 0;
			foreach (var st in statements)
			{
				if (st is Label label)
				{
					label.SetStatement(id);
					offsets[label.Name] = id;
				}
				else if (st is Jump jump)
				{
					jump.Target.SetStatement(offsets[jump.Target.Name]);
				}
				else if (st is JumpIfNot jumpIfNot)
				{
					jumpIfNot.Target.SetStatement(offsets[jumpIfNot.Target.Name]);
				}
				++id;
			}
			return statements;
		}
		private static TextParser<ImmutableArray<IStatement>> CreateCodeParser()
		{
			var statements = IStatement.Parser.ThenIgnore(ParserUtils.OptionalWhitespace).ManyImmutable();
			return statements.Select(FixLabels);
		}

		private static readonly TextParser<ImmutableArray<IStatement>> _codeParser = CreateCodeParser().SuroundOptionalWhitespace();
		private static readonly TextParser<CompiledPou> _parser = CreateParser();
		private static TextParser<CompiledPou> CreateParser()
		{
			TextParser<XmlCompiledPou> xmlParser = input =>
			{
				try
				{
					using (var textStream = new StringReader(input.ToStringValue()))
					{
						var value = (XmlCompiledPou?)_serializer.Deserialize(textStream);
						if(value == null)
							return Superpower.Model.Result.Empty<XmlCompiledPou>(input);
						return Superpower.Model.Result.Value(value, input, Superpower.Model.TextSpan.Empty);
					}
				}
				catch
				{
					return Superpower.Model.Result.Empty<XmlCompiledPou>(input);
				}
			};
			return xmlParser.Select(xml => xml.ToCompiledPou());
		}
		public sealed class XmlCompiledPou
		{
			public sealed class XmlCode
			{
				[System.Xml.Serialization.XmlAttribute("encoding")]
				[System.Diagnostics.CodeAnalysis.AllowNull]
				public string Encoding;
				[System.Xml.Serialization.XmlText]
				[System.Diagnostics.CodeAnalysis.AllowNull]
				public string Text;

				internal ImmutableArray<IStatement> ToCode()
				{
					if (Encoding != "text")
						throw new InvalidOperationException();
					return _codeParser.Parse(Text);
				}
			}
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
					new (new LocalVarOffset(Offset), new Type(Size));
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

			[System.Xml.Serialization.XmlElement("originalPath")]
			public string? OriginalPath;
			[System.Xml.Serialization.XmlElement("breakpoints")]
			public byte[]? Breakpoints;
			[System.Xml.Serialization.XmlArray("variables")]
			public List<XmlVariable>? VariableTable;

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
					Code = new XmlCode()
					{
						Encoding = "text",
						Text = Environment.NewLine + compiled.Code.DelimitWith(Environment.NewLine) + Environment.NewLine
					},
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
		}

		private static readonly System.Xml.Serialization.XmlSerializer _serializer = new (typeof(XmlCompiledPou));
		public static CompiledPou ParsePou(string input) => _parser.Parse(input);

		public static string ToXml(CompiledPou pou)
		{
			var sb = new StringBuilder();
			using var writer = XmlWriter.Create(sb, new()
			{
				Encoding = Encoding.UTF8,
				Indent = true,
			});
			var xml = XmlCompiledPou.FromCompiledPou(pou);
			_serializer.Serialize(writer, xml);
			return sb.ToString();
		}
	}
}
