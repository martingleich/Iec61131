using Superpower;
using Superpower.Parsers;
using System;

namespace Runtime.IR.Expressions
{
	public sealed class LiteralExpression : IExpression
	{
		public readonly ulong Bits;

		public LiteralExpression(ulong bits)
		{
			Bits = bits;
		}

		public void LoadTo(RTE runtime, MemoryLocation location, int size) => runtime.WriteBits(Bits, location, size);

		public static LiteralExpression FromMemoryLocation(MemoryLocation location) => new(BitsFor(location));
		public static ulong BitsFor(MemoryLocation location) => ((uint)location.Area << 16) | location.Offset;

		public static LiteralExpression Bool(bool value) => Bits8(value ? (byte)0 : (byte)255);
		public static LiteralExpression Bits8(byte value) => new(value);
		public static LiteralExpression Bits16(ushort value) => new(value);
		public static LiteralExpression Bits32(uint value) => new(value);
		public static LiteralExpression Bits64(ulong value) => new(value);
		public static LiteralExpression Signed8(sbyte value) => Bits8(unchecked((byte)value));
		public static LiteralExpression Signed16(short value) => Bits16(unchecked((ushort)value));
		public static LiteralExpression Signed32(int value) => Bits32(unchecked((uint)value));
		public static LiteralExpression Signed64(long value) => Bits64(unchecked((ulong)value));
		public static LiteralExpression Float32(float value) => Signed32(BitConverter.SingleToInt32Bits(value));
		public static LiteralExpression Float64(double value) => Signed64(BitConverter.DoubleToInt64Bits(value));
		public static LiteralExpression NullPointer => Bits32(0);
		public override string ToString() => Bits.ToString();
		public static readonly TextParser<IExpression> Parser =
			from _value in Numerics.NaturalUInt64
			select (IExpression)new LiteralExpression(_value);
	}
}
