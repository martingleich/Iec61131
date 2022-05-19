using Superpower;
using Superpower.Parsers;

namespace Runtime.IR
{
	public readonly struct LocalVarOffset
	{
		public readonly ushort Offset;

		public LocalVarOffset(ushort offset)
		{
			Offset = offset;
		}

		public override string ToString() => $"stack{Offset}";

		public static readonly TextParser<LocalVarOffset> Parser =
			from _offset in Span.EqualTo("stack").IgnoreThen(IR.ParserUtils.NaturalUInt16)
			select new LocalVarOffset(_offset);
	}
}
