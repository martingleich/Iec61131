using System;

namespace Compiler
{
	public readonly struct DurationMs32 : IEquatable<DurationMs32>, IComparable<DurationMs32>
	{
		private readonly int Milliseconds;

		public static readonly DurationMs32 Zero = new(0);
		public DurationMs32(int milliseconds)
		{
			Milliseconds = milliseconds;
		}

		public const int MillisecondsPerMillisecond = 1;
		public const int MillisecondsPerSecond = 1_000 * MillisecondsPerMillisecond;
		public const int MillisecondsPerMinute = 60 * MillisecondsPerSecond;
		public const int MillisecondsPerHour = 60 * MillisecondsPerMinute;
		public const int MillisecondsPerDay = 24 * MillisecondsPerHour;

		private static int DivRem(ref int value, int div)
		{
			int result = value / div;
			value -= result * div;
			return result;
		}
		public override string ToString()
		{
			var value = Milliseconds;
			var days = DivRem(ref value, MillisecondsPerDay);
			var hours = DivRem(ref value, MillisecondsPerHour);
			var minutes = DivRem(ref value, MillisecondsPerMinute);
			var seconds = DivRem(ref value, MillisecondsPerSecond);
			var milliseconds = value;

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
			if (result.Length == 0)
				result = "0";
			return $"{result}(={Milliseconds}ms)";
		}

		public bool Equals(DurationMs32 other) => Milliseconds == other.Milliseconds;
		public override bool Equals(object? obj) => throw new NotImplementedException();
		public override int GetHashCode() => Milliseconds.GetHashCode();

		public int CompareTo(DurationMs32 other) => Milliseconds.CompareTo(other.Milliseconds);

		public static bool operator ==(DurationMs32 left, DurationMs32 right) => left.Equals(right);
		public static bool operator !=(DurationMs32 left, DurationMs32 right) => !(left == right);
		public static bool operator <(DurationMs32 left, DurationMs32 right) => left.CompareTo(right) < 0;
		public static bool operator <=(DurationMs32 left, DurationMs32 right) => left.CompareTo(right) <= 0;
		public static bool operator >(DurationMs32 left, DurationMs32 right) => left.CompareTo(right) > 0;
		public static bool operator >=(DurationMs32 left, DurationMs32 right) => left.CompareTo(right) >= 0;
		public DurationMs32 CheckedAdd(DurationMs32 value) => new (checked(Milliseconds + value.Milliseconds));
		public DurationMs32 CheckedSub(DurationMs32 value) => new(checked(Milliseconds - value.Milliseconds));
		public DurationMs32 CheckedNeg() => new(checked(-Milliseconds));
		public DurationMs32 CheckedMod(DurationMs32 value) => new(checked(Milliseconds % value.Milliseconds));
	}
}
