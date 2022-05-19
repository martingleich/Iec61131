using System;

namespace Runtime.IR
{
	public readonly record struct PouId(string Name) : IEquatable<PouId>
	{
		public static PouId ForLoopInit(string type) => new ($"__SYSTEM::FOR_LOOP_INIT_{type}");
		public static PouId ForLoopNext(string type) => new ($"__SYSTEM::FOR_LOOP_NEXT_{type}");
		public override string ToString() => Name;
	}
}
