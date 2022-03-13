using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using StandardLibraryExtensions;
using Superpower;
using Superpower.Parsers;

namespace Runtime.IR
{
	public static class Parser
	{
		static private readonly TextParser<Superpower.Model.TextSpan?> OptionalWhitespace = Span.WhiteSpace.Optional();
		static private readonly TextParser<ushort> NaturalUInt16 = Numerics.NaturalUInt32.Select(v => (ushort)v);
		static private TextParser<T> SuroundOptionalWhitespace<T>(this TextParser<T> parser) =>
			from _1 in OptionalWhitespace
			from value in parser
			from _2 in OptionalWhitespace
			select value;
		static TextParser<T> ConfigLine<T>(string name, TextParser<T> arg) =>
			OptionalWhitespace
			.IgnoreThen(Span.EqualTo($"{name}:"))
			.IgnoreThen(OptionalWhitespace)
			.IgnoreThen(arg);
		static TextParser<(T1, T2)> Tuple<T1, T2>(TextParser<T1> arg1, TextParser<T2> arg2) => (
			from v1 in SuroundOptionalWhitespace(arg1)
			from _5 in Character.In(',')
			from v2 in SuroundOptionalWhitespace(arg2)
			select (v1, v2)).Between(Span.EqualTo('('), Span.EqualTo(')'));
		private static TextParser<ImmutableArray<T>> CommaSeperatedList<T>(this TextParser<T> arg) => new(
			SuroundOptionalWhitespace(arg).ManyDelimitedBy(Character.In(',')).Select(ImmutableArray.Create));
		private static TextParser<ImmutableArray<T>> ManyImmutable<T>(this TextParser<T> arg) => new(
			SuroundOptionalWhitespace(arg).Many().Select(ImmutableArray.Create));
		private static TextParser<T> ThenIgnore<T, U>(this TextParser<T> arg, TextParser<U> ignore) =>
			from _arg in arg
			from _1 in ignore
			select _arg;

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
			// Tokens: [label, return, call, comment, jump, jump if not, copyX, *, <-, Int]
			var stackValue = from _offset in Span.EqualTo("stack").IgnoreThen(NaturalUInt16)
							 select new LocalVarOffset(_offset);
			var expressionDeref = from _value in Span.EqualTo("*").IgnoreThen(stackValue)
								  select (IExpression)new DerefExpression(_value);
			var expressionValue = from _value in stackValue
								  select (IExpression)new LoadValueExpression(_value);
			var expressionLiteral = from _value in Numerics.NaturalUInt64
									select (IExpression)new LiteralExpression(_value);
			var expressionCall = from _value in Numerics.NaturalUInt64
								 select (IExpression)new LiteralExpression(_value);
			var adrBaseStackValue = from _value in stackValue
									select (AddressExpression.IBase)new AddressExpression.BaseStackVar(_value);
			var adrBaseDerefValue = from _value in Span.EqualTo("*").IgnoreThen(stackValue)
									select (AddressExpression.IBase)new AddressExpression.BaseDerefStackVar(_value);
			var adrBase = Parse.OneOf(adrBaseStackValue, adrBaseDerefValue);
			var adrElementOffset = from _value in Span.EqualTo(".").IgnoreThen(NaturalUInt16)
								   select (AddressExpression.IElement)new AddressExpression.ElementOffset(_value);
			var adrElementAnyArray = (from _index in stackValue
										  from _2 in SuroundOptionalWhitespace(Span.EqualTo(","))
										  from _scale in NaturalUInt16
										  from _element in (from _3 in SuroundOptionalWhitespace(Span.EqualTo(","))
															from _lower in Numerics.IntegerInt32
															from _4 in SuroundOptionalWhitespace(Span.EqualTo(","))
															from _upper in Numerics.IntegerInt32
															select (AddressExpression.IElement)new AddressExpression.ElementCheckedArray(_lower, _upper, _index, _scale))
										.OptionalOrDefault(new AddressExpression.ElementUncheckedArray(_index, _scale))
										  select _element).Between(Span.EqualTo("["), Span.EqualTo("]"));
		    var adrElement = Parse.OneOf(adrElementOffset, adrElementAnyArray);
			var expressionAdr = from _1 in Span.EqualTo("&")
								from _base in adrBase
								from _elements in adrElement.ManyImmutable()
								select (IExpression)new AddressExpression(_base, _elements);
			var expression = Parse.OneOf(expressionDeref, expressionValue, expressionLiteral, expressionAdr);
			var statementWriteValue = from _size in Span.EqualTo("copy").IgnoreThen(NaturalUInt16).ThenIgnore(Span.WhiteSpace)
									  from _value in expression
									  from _1 in OptionalWhitespace.ThenIgnore(Span.EqualTo("to")).ThenIgnore(OptionalWhitespace)
									  from _dst in stackValue
									  from _deref in Span.EqualTo("*").Optional()
									  select (IStatement)(_deref.HasValue ? new WriteDerefValue(_value, _dst, _size) : new WriteValue(_value, _dst, _size));
			var statementReturn = Span.EqualTo("return").IgnoreThen(Parse.Return((IStatement)Return.Instance));
			var statementComment = Superpower.Parsers.Comment.ShellStyle.Select(str => (IStatement)new Comment(str.Skip(2).ToStringValue()));
			var label = from arg in Span.NonWhiteSpace select new Label(arg.ToStringValue());
			var statementJump = from _label in Span.EqualTo("jump to").ThenIgnore(Span.WhiteSpace).IgnoreThen(label)
								select (IStatement)new Jump(_label);
			var statementJumpIfNot = from _control in Span.EqualTo("if not").ThenIgnore(Span.WhiteSpace).IgnoreThen(stackValue)
									 from _label in Span.WhiteSpace.IgnoreThen(label)
									 select (IStatement)new JumpIfNot(_control, _label);
			var statementLabel = from _label in Span.EqualTo("label").ThenIgnore(Span.WhiteSpace).IgnoreThen(label)
								 select _label;
			var statementCall = from _callee in Span.EqualTo("call").ThenIgnore(Span.WhiteSpace).IgnoreThen(Span.Except("(")).Select(str => new PouId(str.ToStringValue()))
								from _inputs in stackValue.CommaSeperatedList().Between(Span.EqualTo("("), Span.EqualTo(")"))
								from _1 in Span.EqualTo("=>").SuroundOptionalWhitespace()
								from _outputs in stackValue.CommaSeperatedList()
								select (IStatement)new StaticCall(_callee, _inputs, _outputs);
			var statement = Parse.OneOf(statementComment, statementReturn, statementWriteValue, statementJump).ThenIgnore(OptionalWhitespace);
			var statements = statement.ManyImmutable();
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
						var value = (XmlCompiledPou)_serializer.Deserialize(textStream);
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
				Id = compiled.Id.Callee,
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
