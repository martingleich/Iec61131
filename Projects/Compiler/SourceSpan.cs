using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Compiler
{
	public readonly struct SourceSpan : IEquatable<SourceSpan>
	{
		public static SourceSpan FromStartLength(int start, int length)
		{
			if (start < 0)
				throw new ArgumentException($"{nameof(start)}({start}) must be zero or positive.");
			if (length < 0)
				throw new ArgumentException($"{nameof(length)}({start}) must be zero or positive.");
			if (start > int.MaxValue - length)
				throw new ArgumentException($"End of range must fit into int.");
			return new SourceSpan(start, start + length);
		}
		public static SourceSpan ConvexHull(SourceSpan a, SourceSpan b) => new(
				Math.Min(a.Start, b.Start),
				Math.Max(a.End, b.End));

		private SourceSpan(int start, int end)
		{
			Start = start;
			End = end;
		}

		public readonly int Start;
		public readonly int End;
		public bool Equals(SourceSpan other)
			=> Start == other.Start && End == other.End;
		public override bool Equals(object? obj)
			=> obj is SourceSpan pos && Equals(pos);
		public override int GetHashCode() => HashCode.Combine(Start, End);
		public static bool operator ==(SourceSpan left, SourceSpan right) => left.Equals(right);
		public static bool operator !=(SourceSpan left, SourceSpan right) => !(left == right);
		[ExcludeFromCodeCoverage]
		public override string ToString() => $"{Start}:{End - Start}";
	}


	public static class SourceSpanEx
	{
		public static SourceSpan ConvexHull(this IEnumerable<SourceSpan> self) => self.Aggregate(SourceSpan.ConvexHull);
		public static SourceSpan SourceSpanHull(this IEnumerable<INode> self) => self.Select(self => self.SourceSpan).ConvexHull();
		public static SourceSpan SourceSpanHull(this IEnumerable<IBoundExpression> self) => self.Select(self => self.OriginalNode).SourceSpanHull();
	}
}
