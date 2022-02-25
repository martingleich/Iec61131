using System;

namespace Runtime.IR
{
	public readonly struct PouId : IEquatable<PouId>
	{
		public readonly string Callee;

		public PouId(string callee)
		{
			Callee = callee ?? throw new ArgumentNullException(nameof(callee));
		}
		public static PouId ForLoopInit(string type) => new ($"__SYSTEM.FOR_LOOP_INIT_{type}");
		public static PouId ForLoopNext(string type) => new ($"__SYSTEM.FOR_LOOP_NEXT_{type}");
		public bool Equals(PouId other) => other.Callee.Equals(Callee);
		public override bool Equals(object? obj) => throw new InvalidOperationException();
		public override int GetHashCode() => Callee.GetHashCode();
		public override string ToString() => Callee;

		public static bool operator ==(PouId left, PouId right) => left.Equals(right);
		public static bool operator !=(PouId left, PouId right) => !(left == right);
	}
}
