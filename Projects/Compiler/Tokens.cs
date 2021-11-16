using System;

namespace Compiler
{
	public interface INode
	{
		SourcePosition SourcePosition { get; }
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

	public readonly struct OverflowingInteger
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
	}

	public readonly struct OverflowingReal
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
	public readonly struct OverflowingDuration
	{
		private readonly long Value;
		private readonly bool IsOverflown;

		private OverflowingDuration(long value, bool isOverflown)
		{
			Value = value;
			IsOverflown = isOverflown;
		}
		public static readonly OverflowingDuration Zero = new (0, false);
		public static readonly OverflowingDuration Overflown = new (default, true);
		public static OverflowingDuration FromLongNanoseconds(long value) => new (value, false);
		public static OverflowingDuration FromDoubleNanoseconds(double value)
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

		public static OverflowingDuration UnsignedAdd(OverflowingDuration a, OverflowingDuration b)
		{
			if (a.Value < 0 || b.Value < 0)
				throw new ArgumentException();
			if (a.IsOverflown || b.IsOverflown)
				return Overflown;
			long value;
			try
			{
				value = checked(a.Value + b.Value);
			}
			catch (OverflowException)
			{
				return Overflown;
			}
			return FromLongNanoseconds(value);
		}
		public override string ToString() => IsOverflown ? "Overflown" : Value.ToString();
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
