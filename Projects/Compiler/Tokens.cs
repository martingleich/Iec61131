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

	public struct TypedLiteral
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

	public struct OverflowingInteger
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
	}

	public struct OverflowingReal
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

	public struct OverflowingDate
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
	public struct OverflowingDuration
	{
		private readonly TimeSpan Value;
		private readonly bool IsOverflown;

		private OverflowingDuration(TimeSpan value, bool isOverflown)
		{
			Value = value;
			IsOverflown = isOverflown;
		}
		public static readonly OverflowingDuration Overflown = new (default, true);
		public static OverflowingDuration FromDuration(TimeSpan value) => new (value, false);

		public override string ToString() => IsOverflown ? "Overflown" : Value.ToString();
	}
	public struct OverflowingDateAndTime
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
