using System;

namespace Compiler
{
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
}
