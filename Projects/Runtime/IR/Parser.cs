using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using StandardLibraryExtensions;
using Superpower;

namespace Runtime.IR
{
	using Statements;
	using Expressions;
	using System.IO;
	using System.IO.Compression;
	using System.ComponentModel.DataAnnotations;

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
				[System.Xml.Serialization.XmlAttribute("id")]
				public int Id;
				[System.Xml.Serialization.XmlAttribute("offset")]
				public int Offset;

				internal static XmlArg FromTuple((LocalVarOffset, int) arg)
					=> new()
					{
						Id = arg.Item2,
						Offset = arg.Item1.Offset
					};

				internal (LocalVarOffset, int) ToTuple()
				{
					return (new LocalVarOffset((ushort)Offset), Id);
				}
			}

			[System.Xml.Serialization.XmlType("breakpoint")]
			public sealed class XmlBreakpoint
			{
				[System.Xml.Serialization.XmlAttribute("id")]
				public int Id;
				[System.Xml.Serialization.XmlAttribute("startLine")]
				public int StartLine;
				[System.Xml.Serialization.XmlAttribute("startCollumn")]
				public int StartCollumn;
				[System.Xml.Serialization.XmlAttribute("endLine")]
				public int EndLine;
				[System.Xml.Serialization.XmlAttribute("endCollumn")]
				public int EndCollumn;
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
				};
			}

			public CompiledPou ToCompiledPou()
			{
				return new(
					new PouId(Id),
					Code.ToCode(),
					Inputs.Select(input => input.ToTuple()).ToImmutableArray(),
					Outputs.Select(input => input.ToTuple()).ToImmutableArray(),
					StackUsage)
				{
					BreakpointMap = ToBreakpointsMap(Breakpoints),
					OriginalPath = OriginalPath
				};
			}
		}

		private static readonly System.Xml.Serialization.XmlSerializer _serializer = new (typeof(XmlCompiledPou));
		public static CompiledPou ParsePou(string input) => _parser.Parse(input);

		public static string ToXml(CompiledPou pou)
		{
			var xml = XmlCompiledPou.FromCompiledPou(pou);
			using var textStream = new System.IO.StringWriter();
			_serializer.Serialize(textStream, xml);
			return textStream.ToString();
		}
	}
}
