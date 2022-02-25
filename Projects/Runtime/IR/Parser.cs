using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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

		private static readonly TextParser<CompiledPou> _parser = CreateParser();
		private static TextParser<CompiledPou> CreateParser()
		{
			// file := s* line<id, identifier> line<inputs, args> line<outputs, args>
			// line<Name, Data> := "{Name}:" s* {Data} s*
			// args := ("(" s* Int32 s* "," s* Int32 s* ")")*;
			// 
			var id = ConfigLine("id", Span.NonWhiteSpace.Select(str => new PouId(str.ToStringValue())));
			var localVarArg = NaturalUInt16.Select(var => new LocalVarOffset(var));
			var intArg = Numerics.IntegerInt32;
			var inoutArg = Tuple(localVarArg, intArg);
			var inputs = ConfigLine("inputs", CommaSeperatedList(inoutArg));
			var outputs = ConfigLine("outputs", CommaSeperatedList(inoutArg));
			var stackusage = ConfigLine("stackusage", intArg);
			var code = CreateCodeParser();

			return from _id in id
				   from _inputs in inputs
				   from _outputs in outputs
				   from _stackusage in stackusage
				   from _1 in Span.WhiteSpace
				   from _code in code
				   from _3 in OptionalWhitespace
				   select new CompiledPou(_id, _code, _inputs, _outputs, _stackusage);
		}

		public static CompiledPou ParsePou(string input) => _parser.Parse(input);
	}
}
