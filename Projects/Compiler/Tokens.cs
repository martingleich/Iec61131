using System;
using System.Numerics;

namespace Compiler
{
	public interface INode
	{
		SourceSpan SourceSpan { get; }
	}
	public interface IToken : INode
	{
		int StartPosition { get; }
		int Length { get; }
		string Generating { get; }
		IToken? LeadingNonSyntax { get; }
		IToken? TrailingNonSyntax { get; set; }
	}
	public interface ITokenWithValue<out T> : IToken
	{
		T Value { get; }
	}

	public readonly struct TypedLiteral
	{
		public readonly IBuiltInTypeToken Type;
		public readonly ILiteralToken LiteralToken;

		public TypedLiteral(IBuiltInTypeToken type, ILiteralToken literalToken)
		{
			Type = type;
			LiteralToken = literalToken;
		}

		public override string? ToString() => $"{Type}#{LiteralToken}";
	}

	public readonly struct OverflowingInteger : IEquatable<OverflowingInteger>
	{
		private readonly ulong Value;
		private readonly bool IsNegative;
		private readonly bool IsOverflown;

		private OverflowingInteger(ulong value, bool isNegative, bool isOverflown)
		{
			Value = value;
			IsNegative = isNegative;
			IsOverflown = isOverflown;
		}
		public static readonly OverflowingInteger Overflown = new (default, false, true);
		public static OverflowingInteger FromLong(long value)
		{
			if (value < 0)
			{
				ulong x = unchecked(~(ulong)value + 1ul);
				return new(x, true, false);
			}
			else
			{
				return new((ulong)value, false, false);
			}
		}
		public static OverflowingInteger FromULong(ulong value) => new(value, false, false);
		public static OverflowingInteger FromUlong(ulong value, bool isNegative) => new (value, isNegative, false);

		public static OverflowingInteger Parse(string input, bool isNegative)
		{
			ulong value = 0;
			foreach (var c in input)
			{
				if (c != '_')
				{
					if (c < '0' || c > '9')
						throw new ArgumentException($"Invalid character in string '{c}'", nameof(input));
					var d = (ulong)c - '0';
					if (value > (ulong.MaxValue - d) / 10)
						return Overflown;
					value = value * 10 + d;
				}
			}
			return FromUlong(value, isNegative);
		}
		public override string ToString() => IsOverflown ? "Overflown" : ((IsNegative ? "-" : "") + Value.ToString());

		public bool TryGetSByte(out sbyte value)
		{
			if (TryGetInt(out int iValue) && iValue <= sbyte.MaxValue && iValue >= sbyte.MinValue)
			{
				value = (sbyte)iValue;
				return true;
			}
			else
			{
				value = 0;
				return false;
			}
		}
		public bool TryGetByte(out byte value)
		{
			if (TryGetUInt(out var iValue) && iValue <= byte.MaxValue)
			{
				value = (byte)iValue;
				return true;
			}
			else
			{
				value = 0;
				return false;
			}
		}
		public bool TryGetShort(out short value)
		{
			if (TryGetInt(out int iValue) && iValue <= short.MaxValue && iValue >= short.MinValue)
			{
				value = (short)iValue;
				return true;
			}
			else
			{
				value = 0;
				return false;
			}
		}
		public bool TryGetUShort(out ushort value)
		{
			if (TryGetUInt(out var iValue) && iValue <= ushort.MaxValue)
			{
				value = (ushort)iValue;
				return true;
			}
			else
			{
				value = 0;
				return false;
			}
		}
		public bool TryGetInt(out int value)
		{
			if (TryGetLong(out var iValue) && iValue <= int.MaxValue && iValue >= int.MinValue)
			{
				value = (int)iValue;
				return true;
			}
			else
			{
				value = 0;
				return false;
			}
		}
		public bool TryGetUInt(out uint value)
		{
			if (TryGetULong(out var iValue) && iValue <= uint.MaxValue)
			{
				value = (uint)iValue;
				return true;
			}
			else
			{
				value = 0;
				return false;
			}
		}
		public bool TryGetLong(out long value)
		{
			if (!IsOverflown)
			{
				if (!IsNegative && Value <= long.MaxValue)
				{
					value = (long)Value;
					return true;
				}
				else if (IsNegative && Value <= (ulong)long.MaxValue + 1)
				{
					value = unchecked(-(long)Value);
					return true;
				}
				else
				{
					value = 0;
					return false;
				}
			}
			else
			{
				value = 0;
				return false;
			}
		}
		public bool TryGetULong(out ulong value)
		{
			if (!IsOverflown && !IsNegative && Value <= ulong.MaxValue)
			{
				value = Value;
				return true;
			}
			else
			{
				value = 0;
				return false;
			}
		}
		public bool TryGetSingle(out float value)
		{
			value = Value;
			if (float.IsFinite(value))
			{
				if (IsNegative)
					value = -value;
				return true;
			}
			else
			{
				value = 0;
				return false;
			}
		}
		public bool TryGetDouble(out double value)
		{
			value = Value;
			if (double.IsFinite(value))
			{
				if (IsNegative)
					value = -value;
				return true;
			}
			else
			{
				value = 0;
				return false;
			}
		}
		
		public bool IsZero => TryGetInt(out var value) && value == 0;
		public bool IsOne => TryGetInt(out var value) && value == 1;
		public OverflowingInteger GetNegative() => new (Value, !IsNegative, IsOverflown);

		public bool Equals(OverflowingInteger other)
		{
			if (IsOverflown != other.IsOverflown)
				return false;
			if (IsNegative != other.IsNegative)
				return Value == 0;
			return Value == other.Value;
		}
		public override bool Equals(object? obj) => throw new NotImplementedException();
		public override int GetHashCode() => HashCode.Combine(Value);

		public static bool operator ==(OverflowingInteger left, OverflowingInteger right) => left.Equals(right);
		public static bool operator !=(OverflowingInteger left, OverflowingInteger right) => !(left == right);
	}

	public readonly struct OverflowingReal : IEquatable<OverflowingReal>
	{
		private readonly double Value;
		private readonly bool IsOverflown;

		private OverflowingReal(double value, bool isOverflown)
		{
			Value = value;
			IsOverflown = isOverflown;
		}
		public static readonly OverflowingReal Overflown = new (default, true);
		public static OverflowingReal FromDouble(double value) => new (value, false);
		public bool TryGetDouble(out double value)
		{
			if (IsOverflown)
			{
				value = 0;
				return false;
			}
			else
			{
				value = Value;
				return true;
			}
		}
		public bool TryGetSingle(out float value)
		{
			value = (float)Value;
			if (IsOverflown || !float.IsFinite(value))
			{
				value = 0;
				return false;
			}
			else
			{
				return true;
			}
		}

		public static OverflowingReal Parse(string pureValue)
		{
			pureValue = pureValue.Replace("_", "");
			var value = double.Parse(pureValue, System.Globalization.NumberFormatInfo.InvariantInfo);
			if (double.IsFinite(value))
				return FromDouble(value);
			else
				return Overflown;
		}

		public override string ToString() => IsOverflown ? "Overflown" : Value.ToString();

		public bool Equals(OverflowingReal other)
		{
			if (IsOverflown != other.IsOverflown)
				return false;
			return Value == other.Value;
		}

		public override bool Equals(object? obj) => throw new NotImplementedException();
		public override int GetHashCode() => Value.GetHashCode();

		public static bool operator ==(OverflowingReal left, OverflowingReal right) => left.Equals(right);
		public static bool operator !=(OverflowingReal left, OverflowingReal right) => !(left == right);
	}

	public readonly struct OverflowingDate
	{
		private readonly DateTime Value;
		private readonly bool IsOverflown;

		private OverflowingDate(DateTime value, bool isOverflown)
		{
			Value = value;
			IsOverflown = isOverflown;
		}
		public static readonly OverflowingDate Overflown = new (default, true);
		public static OverflowingDate FromDateTime(DateTime value) => new (value, false);

		public override string ToString() => IsOverflown ? "Overflown" : Value.ToString();
	}

	public readonly struct DurationNs64 : IEquatable<DurationNs64>, IComparable<DurationNs64>
	{
		private readonly long Nanoseconds;

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
	
	public readonly struct OverflowingDateAndTime
	{
		private readonly DateTime Value;
		private readonly bool IsOverflown;

		private OverflowingDateAndTime(DateTime value, bool isOverflown)
		{
			Value = value;
			IsOverflown = isOverflown;
		}
		public static readonly OverflowingDateAndTime Overflown = new (default, true);
		public static OverflowingDateAndTime FromDateTime(DateTime value) => new (value, false);

		public override string ToString() => IsOverflown ? "Overflown" : Value.ToString();
	}
}
