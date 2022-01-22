using System;

namespace Compiler
{
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
}
