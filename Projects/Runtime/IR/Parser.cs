﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using StandardLibraryExtensions;
using Superpower;

namespace Runtime.IR
{
	using Statements;
	using Expressions;
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
					using (var textStream = new System.IO.StringReader(input.ToStringValue()))
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
				public string Encoding;
				[System.Xml.Serialization.XmlText]
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
			[System.Xml.Serialization.XmlAttribute("id")]
			public string Id;
			[System.Xml.Serialization.XmlArray("inputs")]
			public List<XmlArg> Inputs;
			[System.Xml.Serialization.XmlArray("outputs")]
			public List<XmlArg> Outputs;
			[System.Xml.Serialization.XmlElement("stackusage")]
			public int StackUsage;
			[System.Xml.Serialization.XmlElement("code")]
			public XmlCode Code;

			public static XmlCompiledPou FromCompiledPou(CompiledPou compiled) => new()
			{
				Id = compiled.Id.Name,
				Inputs = compiled.InputArgs.Select(XmlArg.FromTuple).ToList(),
				Outputs = compiled.OutputArgs.Select(XmlArg.FromTuple).ToList(),
				StackUsage = compiled.StackUsage,
				Code = new XmlCode()
				{
					Encoding = "text",
					Text = Environment.NewLine + compiled.Code.DelimitWith(Environment.NewLine) + Environment.NewLine
				}
			};

			public CompiledPou ToCompiledPou()
			{
				return new CompiledPou(
					new PouId(Id),
					Code.ToCode(),
					Inputs.Select(input => input.ToTuple()).ToImmutableArray(),
					Outputs.Select(input => input.ToTuple()).ToImmutableArray(),
					StackUsage);
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