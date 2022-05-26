using System;
using System.Collections.Generic;

namespace Runtime.IR
{
    public readonly struct Range<T> where T : IComparable<T>
	{
		public readonly T Start;
		public readonly T End;

		public Range(T start, T end)
		{
			if(start.CompareTo(end) > 0)
				throw new ArgumentException($"{nameof(start)}({start}) must be smaller or equal to {nameof(end)}({end}).");
			Start = start;
			End = end;
		}
		public override string ToString() => $"{Start}..{End}";
		public bool Contains(T value) => Start.CompareTo(value) <= 0 && End.CompareTo(value) > 0;
	}
    public static class Range
	{
		public static Range<T> Create<T>(T start, T end) where T : IComparable<T> => new(start, end);
		public static IEnumerable<int> ToEnumerable(this Range<int> values) => values.ToEnumerable(x => x + 1);
		public static IEnumerable<T> ToEnumerable<T>(this Range<T> values, Func<T, T> next) where T : IComparable<T>
		{
			var cur = values.Start;
			while (cur.CompareTo(values.End) < 0)
			{
				yield return cur;
				cur = next(cur);
			}
		}
	
		public static int GetLength(this Range<int> range) => range.End - range.Start;
	}

    public sealed class RangeKeyArrayComparer<TKey, TValue> : IComparer<KeyValuePair<Range<TKey>, TValue>> where TKey:IComparable<TKey>
	{
		public static readonly RangeKeyArrayComparer<TKey, TValue> Instance = new();
		public int Compare(KeyValuePair<Range<TKey>, TValue> x, KeyValuePair<Range<TKey>, TValue> y) => x.Key.Start.CompareTo(y.Key.Start);
	}
}
