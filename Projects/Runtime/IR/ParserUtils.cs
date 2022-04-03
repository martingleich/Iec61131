using System.Collections.Immutable;
using System.Linq;
using Superpower;
using Superpower.Parsers;

namespace Runtime.IR
{
	public static class ParserUtils
	{
		public static readonly TextParser<Superpower.Model.TextSpan?> OptionalWhitespace = Span.WhiteSpace.Optional();
		public static readonly TextParser<ushort> NaturalUInt16 = Numerics.NaturalUInt32.Where(v => v <= ushort.MaxValue, "Number to big for ushort").Select(v => (ushort)v);
		public static TextParser<T> SuroundOptionalWhitespace<T>(this TextParser<T> parser) =>
			from _1 in OptionalWhitespace
			from value in parser
			from _2 in OptionalWhitespace
			select value;
		public static TextParser<(T1, T2)> Tuple<T1, T2>(TextParser<T1> arg1, TextParser<T2> arg2) => (
			from v1 in SuroundOptionalWhitespace(arg1)
			from _5 in Character.In(',')
			from v2 in SuroundOptionalWhitespace(arg2)
			select (v1, v2)).Between(Span.EqualTo('('), Span.EqualTo(')'));
		public static TextParser<ImmutableArray<T>> CommaSeperatedList<T>(this TextParser<T> arg) => new(
			SuroundOptionalWhitespace(arg).ManyDelimitedBy(Character.In(',')).Select(ImmutableArray.Create));
		public static TextParser<ImmutableArray<T>> ManyImmutable<T>(this TextParser<T> arg) => new(
			SuroundOptionalWhitespace(arg).Many().Select(ImmutableArray.Create));
		public static TextParser<T> ThenIgnore<T, U>(this TextParser<T> arg, TextParser<U> ignore) =>
			from _arg in arg
			from _1 in ignore
			select _arg;

	}
}
