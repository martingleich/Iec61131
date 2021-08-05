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

	public struct OverflowingInteger
	{
		private readonly ulong Value;
		private readonly bool IsOverflown;

		private OverflowingInteger(ulong value, bool isOverflown)
		{
			Value = value;
			IsOverflown = isOverflown;
		}
		public static readonly OverflowingInteger Overflown = new (default, true);
		public static OverflowingInteger FromUlong(ulong value) => new (value, false);

		public override string ToString() => IsOverflown ? "Overflown" : Value.ToString();
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
