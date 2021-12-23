using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Compiler
{
	public readonly struct SourceSpan : IEquatable<SourceSpan>
	{
		public static SourceSpan FromStartLength(int start, int length) => FromStartLength(new SourcePoint(start), length);
		public static SourceSpan FromStartLength(SourcePoint start, int length)
		{
			if (length < 0)
				throw new ArgumentException($"{nameof(length)}({start}) must be zero or positive.");
			if (start.Offset > int.MaxValue - length)
				throw new ArgumentException($"End of range must fit into int.");
			return new SourceSpan(start, length);
		}
		public static SourceSpan FromStartEnd(SourcePoint start, SourcePoint end)
		{
			if(end < start)
				throw new ArgumentException($"{nameof(start)}({start}) must be before {nameof(end)}({end}).");
			return new SourceSpan(start, end.Offset - start.Offset);
		}
		public static SourceSpan ConvexHull(SourceSpan a, SourceSpan b) => FromStartEnd(
				SourcePoint.Min(a.Start, b.Start),
				SourcePoint.Max(a.End, b.End));

		private SourceSpan(SourcePoint start, int length)
		{
			Start = start;
			Length = length;
		}

		public readonly SourcePoint Start;
		public SourcePoint End => Start.PlusOffset(Length);
		public readonly int Length;
		public bool Equals(SourceSpan other)
			=> Start == other.Start && Length == other.Length;
		public override bool Equals(object? obj)
			=> obj is SourceSpan pos && Equals(pos);
		public override int GetHashCode() => HashCode.Combine(Start, Length);
		public static bool operator ==(SourceSpan left, SourceSpan right) => left.Equals(right);
		public static bool operator !=(SourceSpan left, SourceSpan right) => !(left == right);
		[ExcludeFromCodeCoverage]
		public override string ToString() => $"{Start}:{Length}";
	}


	public static class SourceSpanEx
	{
		public static SourceSpan ConvexHull(this IEnumerable<SourceSpan> self) => self.Aggregate(SourceSpan.ConvexHull);
		public static SourceSpan SourceSpanHull(this IEnumerable<INode> self) => self.Select(self => self.SourceSpan).ConvexHull();
		public static SourceSpan SourceSpanHull(this IEnumerable<IBoundExpression> self) => self.Select(self => self.OriginalNode).SourceSpanHull();
	}
}
