using System;
using System.Numerics;

namespace Compiler
{
	public readonly struct OverflowingDuration : IEquatable<OverflowingDuration>
	{
		private readonly DurationNs64 Value;
		private readonly bool IsOverflown;

		private OverflowingDuration(DurationNs64 value, bool isOverflown)
		{
			Value = value;
			IsOverflown = isOverflown;
		}
		public static readonly OverflowingDuration Zero = new (DurationNs64.Zero, false);
		public static readonly OverflowingDuration Overflown = new (default, true);
		public static OverflowingDuration FromLongNanoseconds(long value) => new (new DurationNs64(value), false);
		public static OverflowingDuration FromBigIntegerNanoseconds(BigInteger value)
		{
			long longValue;
			try
			{
				longValue = checked((long)value);
			}
			catch (OverflowException)
			{
				return Overflown;
			}
			return FromLongNanoseconds(longValue);
		}

		public bool TryGetDurationNs64(out DurationNs64 value)
		{
			if (IsOverflown)
			{
				value = default;
				return false;
			}
			else
			{
				value = Value;
				return true;
			}
		}
		public bool TryGetDurationMs32(out DurationMs32 value)
		{
			if (IsOverflown)
			{
				value = default;
				return false;
			}
			else
			{
				return Value.TryGetMs32(out value);
			}
		}

		public override string ToString() => IsOverflown ? "Overflown" : Value.ToString();

		public bool Equals(OverflowingDuration other)
		{
			return IsOverflown == other.IsOverflown && Value.Equals(other.Value);
		}
		public override bool Equals(object? obj) => throw new NotImplementedException();
		public override int GetHashCode() => Value.GetHashCode();

		public static bool operator ==(OverflowingDuration left, OverflowingDuration right) => left.Equals(right);
		public static bool operator !=(OverflowingDuration left, OverflowingDuration right) => !(left == right);
	}
}
