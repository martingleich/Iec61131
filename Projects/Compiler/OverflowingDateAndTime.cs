using System;

namespace Compiler
{
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
