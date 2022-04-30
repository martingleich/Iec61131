using System;

namespace Runtime.IR
{
    public readonly struct SourceLC : IComparable<SourceLC>
	{
		public readonly int Line;
		public readonly int Collumn;

		public SourceLC(int line, int collumn)
		{
			Line = line;
			Collumn = collumn;
		}

		public int CompareTo(SourceLC other)
		{
			int x;
			x = Line.CompareTo(other.Line);
			if (x != 0)
				return x;
			if (Collumn < 0 || other.Collumn < 0)
				return 0;
			return Collumn.CompareTo(other.Collumn);
		}
		public override string ToString() => $"{Line}:{Collumn}";
	}
}
