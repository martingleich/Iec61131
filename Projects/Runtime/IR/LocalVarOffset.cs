using Superpower;
using Superpower.Parsers;
using System;

namespace Runtime.IR
{
	public readonly struct LocalVarOffset : IEquatable<LocalVarOffset>, IComparable<LocalVarOffset>
    {
		public readonly ushort Offset;

		public LocalVarOffset(ushort offset)
		{
			Offset = offset;
		}

		public override string ToString() => $"stack{Offset}";

        public override bool Equals(object? obj)
        {
            return obj is LocalVarOffset offset && Equals(offset);
        }

        public bool Equals(LocalVarOffset other)
        {
            return Offset == other.Offset;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Offset);
        }

        public int CompareTo(LocalVarOffset other) => Offset.CompareTo(other.Offset);

        public static readonly TextParser<LocalVarOffset> Parser =
			from _offset in Span.EqualTo("stack").IgnoreThen(IR.ParserUtils.NaturalUInt16)
			select new LocalVarOffset(_offset);

        public static bool operator ==(LocalVarOffset left, LocalVarOffset right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LocalVarOffset left, LocalVarOffset right)
        {
            return !(left == right);
        }

        public static bool operator <(LocalVarOffset left, LocalVarOffset right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator <=(LocalVarOffset left, LocalVarOffset right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >(LocalVarOffset left, LocalVarOffset right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator >=(LocalVarOffset left, LocalVarOffset right)
        {
            return left.CompareTo(right) >= 0;
        }
        public static LocalVarOffset operator +(LocalVarOffset offset, ushort plus) => new (checked((ushort)(offset.Offset + plus)));
        public static LocalVarOffset operator -(LocalVarOffset offset, ushort plus) => new (checked((ushort)(offset.Offset - plus)));
    }
}
