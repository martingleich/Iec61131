using System;

namespace Compiler
{
	public readonly struct SourcePoint : IComparable<SourcePoint>, IEquatable<SourcePoint>
	{
		public static readonly SourcePoint Null = new (0);
		public readonly int Offset;

		internal static SourcePoint FromOffset(int offset) => new (offset);

		private SourcePoint(int offset)
		{
			if (offset < 0)
				throw new ArgumentException($"{nameof(offset)}({offset}) must be zero or positive.");
			Offset = offset;
		}

		public SourcePoint PlusOffset(int offset) => new (checked(Offset + offset));

		public override string ToString() => Offset.ToString();
		public override bool Equals(object? obj) => throw new NotImplementedException();
		public bool Equals(SourcePoint other) => Offset.Equals(other.Offset);
		public int CompareTo(SourcePoint other) => Offset.CompareTo(other.Offset);
		public override int GetHashCode() => Offset.GetHashCode();

		public static bool operator ==(SourcePoint left, SourcePoint right) => left.Equals(right);
		public static bool operator !=(SourcePoint left, SourcePoint right) => !(left == right);
		public static bool operator <(SourcePoint left, SourcePoint right) => left.CompareTo(right) < 0;
		public static bool operator <=(SourcePoint left, SourcePoint right) => left.CompareTo(right) <= 0;
		public static bool operator >(SourcePoint left, SourcePoint right) => left.CompareTo(right) > 0;
		public static bool operator >=(SourcePoint left, SourcePoint right) => left.CompareTo(right) >= 0;

		public static SourcePoint Min(SourcePoint a, SourcePoint b) => new(Math.Min(a.Offset, b.Offset));
		public static SourcePoint Max(SourcePoint a, SourcePoint b) => new(Math.Max(a.Offset, b.Offset));
	}
}
