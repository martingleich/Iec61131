using System;

namespace Compiler
{
	public readonly struct DurationNs64 : IEquatable<DurationNs64>, IComparable<DurationNs64>
	{
		public readonly long Nanoseconds;

		public static readonly DurationNs64 Zero = new(0);
		public DurationNs64(long nanoseconds)
		{
			Nanoseconds = nanoseconds;
		}

		public const long NanosecondsPerMicrosecond = 1_000;
		public const long NanosecondsPerMillisecond = 1_000 * NanosecondsPerMicrosecond;
		public const long NanosecondsPerSecond = 1_000 * NanosecondsPerMillisecond;
		public const long NanosecondsPerMinute = 60L * NanosecondsPerSecond;
		public const long NanosecondsPerHour = 60L * NanosecondsPerMinute;
		public const long NanosecondsPerDay = 24L * NanosecondsPerHour;

		private static long DivRem(ref long value, long div)
		{
			long result = value / div;
			value -= result * div;
			return result;
		}
		public override string ToString()
		{
			var value = Nanoseconds;
			var days = DivRem(ref value, NanosecondsPerDay);
			var hours = DivRem(ref value, NanosecondsPerHour);
			var minutes = DivRem(ref value, NanosecondsPerMinute);
			var seconds = DivRem(ref value, NanosecondsPerSecond);
			var milliseconds = DivRem(ref value, NanosecondsPerMillisecond);
			var microseconds = DivRem(ref value, NanosecondsPerMicrosecond);
			var nanoseconds = value;

			string result = "";
			if (days != 0)
				result += $"{days}d";
			if (hours != 0)
				result += $"{hours}h";
			if (minutes != 0)
				result += $"{minutes}m";
			if (seconds != 0)
				result += $"{seconds}s";
			if (milliseconds != 0)
				result += $"{milliseconds}ms";
			if (microseconds != 0)
				result += $"{microseconds}us";
			if (nanoseconds != 0)
				result += $"{nanoseconds}ns";
			if (result.Length == 0)
				result = "0";
			return $"{result}(={Nanoseconds}ns)";
		}

		public bool Equals(DurationNs64 other) => Nanoseconds == other.Nanoseconds;
		public override bool Equals(object? obj) => throw new NotImplementedException();
		public override int GetHashCode() => Nanoseconds.GetHashCode();

		public static bool operator ==(DurationNs64 left, DurationNs64 right) => left.Equals(right);
		public static bool operator !=(DurationNs64 left, DurationNs64 right) => !(left == right);

		public bool TryGetMs32(out DurationMs32 value)
		{
			var milliseconds = Nanoseconds / NanosecondsPerMillisecond;
			try
			{
				value = new DurationMs32(checked((int)milliseconds));
				return true;
			}
			catch (OverflowException)
			{
				value = default;
				return false;
			}
		}

		public int CompareTo(DurationNs64 other) => Nanoseconds.CompareTo(other.Nanoseconds);
		public static bool operator <(DurationNs64 left, DurationNs64 right) => left.CompareTo(right) < 0;
		public static bool operator <=(DurationNs64 left, DurationNs64 right) => left.CompareTo(right) <= 0;
		public static bool operator >(DurationNs64 left, DurationNs64 right) => left.CompareTo(right) > 0;
		public static bool operator >=(DurationNs64 left, DurationNs64 right) => left.CompareTo(right) >= 0;

		public DurationNs64 CheckedAdd(DurationNs64 value) => new (checked(Nanoseconds + value.Nanoseconds));
		public DurationNs64 CheckedSub(DurationNs64 value) => new(checked(Nanoseconds - value.Nanoseconds));
		public DurationNs64 CheckedNeg() => new(checked(-Nanoseconds));
		public DurationNs64 CheckedMod(DurationNs64 value) => new(checked(Nanoseconds % value.Nanoseconds));
	}
}
