using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Compiler
{
	public readonly struct DurationUnit
	{
		private readonly int Id;
		public long Factor => TimeUnitScales[Id];
		private static readonly ImmutableArray<CaseInsensitiveString> TimeUnitNames = ImmutableArray.Create(
			"d".ToCaseInsensitive(),
			"h".ToCaseInsensitive(),
			"m".ToCaseInsensitive(),
			"s".ToCaseInsensitive(),
			"ms".ToCaseInsensitive(),
			"us".ToCaseInsensitive(),
			"ns".ToCaseInsensitive());
		private static readonly ImmutableArray<long> TimeUnitScales = ImmutableArray.Create(
			DurationNs64.NanosecondsPerDay,
			DurationNs64.NanosecondsPerHour,
			DurationNs64.NanosecondsPerMinute,
			DurationNs64.NanosecondsPerSecond,
			DurationNs64.NanosecondsPerMillisecond,
			DurationNs64.NanosecondsPerMicrosecond,
			1L);

		public DurationUnit(int id)
		{
			Id = id;
		}

		public static DurationUnit? TryMap(CaseInsensitiveString name)
		{
			for (int i = 0; i < TimeUnitNames.Length; ++i)
			{
				if (name == TimeUnitNames[i])
					return new DurationUnit(i);
			}
			return null;
		}

		[ExcludeFromCodeCoverage]
		public override bool Equals(object? obj) => throw new System.NotImplementedException();
		[ExcludeFromCodeCoverage]
		public override int GetHashCode() => throw new System.NotImplementedException();
		[ExcludeFromCodeCoverage]
		public override string ToString() => TimeUnitNames[Id].Original;
	}
}
