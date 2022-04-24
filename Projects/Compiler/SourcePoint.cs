using System;

namespace Compiler
{
	public readonly struct SourcePoint : IComparable<SourcePoint>, IEquatable<SourcePoint>
	{
		public static readonly SourcePoint Null = new(null, 0);
		public readonly int Offset;
		public readonly string? File;

		public static SourcePoint FromOffset(string file, int offset)
		{
			if (file is null)
				throw new ArgumentNullException(nameof(file));
			return new(file, offset);
		}

		private SourcePoint(string? file, int offset)
		{
			if (offset < 0)
				throw new ArgumentException($"{nameof(offset)}({offset}) must be zero or positive.");
			File = file;
			Offset = offset;
		}

		public SourcePoint PlusOffset(int offset) => new(File, checked(Offset + offset));

		public override string ToString() => $"{File}:{Offset}";
		public override bool Equals(object? obj) => throw new NotImplementedException();
		public bool Equals(SourcePoint other) => string.Equals(File, other.File) &&  Offset.Equals(other.Offset);
		public int CompareTo(SourcePoint other)
		{
			switch (string.Compare(File, other.File))
			{
				case 0: return Offset.CompareTo(other.Offset);
				case int x: return x;
			}
		}
		public override int GetHashCode() => HashCode.Combine(File, Offset);

		public static bool operator ==(SourcePoint left, SourcePoint right) => left.Equals(right);
		public static bool operator !=(SourcePoint left, SourcePoint right) => !(left == right);
		public static bool operator <(SourcePoint left, SourcePoint right) => left.CompareTo(right) < 0;
		public static bool operator <=(SourcePoint left, SourcePoint right) => left.CompareTo(right) <= 0;
		public static bool operator >(SourcePoint left, SourcePoint right) => left.CompareTo(right) > 0;
		public static bool operator >=(SourcePoint left, SourcePoint right) => left.CompareTo(right) >= 0;

		public static SourcePoint operator +(SourcePoint point, int offset) => point.PlusOffset(offset);
		public static SourcePoint operator -(SourcePoint point, int offset) => point.PlusOffset(-offset);

		private static string? MergeFiles(string? a, string? b)
		{
			if (a != b)
				throw new ArgumentException("Diffrent files cannot be merged");
			return a;
		}

		public static SourcePoint Min(SourcePoint a, SourcePoint b) => new(MergeFiles(a.File, b.File), Math.Min(a.Offset, b.Offset));
		public static SourcePoint Max(SourcePoint a, SourcePoint b) => new(MergeFiles(a.File, b.File), Math.Max(a.Offset, b.Offset));
	}
}
