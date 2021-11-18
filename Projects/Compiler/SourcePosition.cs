using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Compiler
{
	public readonly struct SourcePosition : IEquatable<SourcePosition>
	{
		public static SourcePosition FromStartLength(int start, int length)
		{
			if (start < 0)
				throw new ArgumentException($"{nameof(start)}({start}) must be zero or positive.");
			if (length < 0)
				throw new ArgumentException($"{nameof(length)}({start}) must be zero or positive.");
			if (start > int.MaxValue - length)
				throw new ArgumentException($"End of range must fit into int.");
			return new SourcePosition(start, start + length);
		}
		public static SourcePosition ConvexHull(SourcePosition a, SourcePosition b) => new(
				Math.Min(a.Start, b.Start),
				Math.Max(a.End, b.End));

		private SourcePosition(int start, int end)
		{
			Start = start;
			End = end;
		}

		public readonly int Start;
		public readonly int End;
		public bool Equals(SourcePosition other)
			=> Start == other.Start && End == other.End;
		public override bool Equals(object? obj)
			=> obj is SourcePosition pos && Equals(pos);
		public override int GetHashCode() => HashCode.Combine(Start, End);
		public static bool operator ==(SourcePosition left, SourcePosition right) => left.Equals(right);
		public static bool operator !=(SourcePosition left, SourcePosition right) => !(left == right);
		[ExcludeFromCodeCoverage]
		public override string ToString() => $"{Start}:{End - Start}";
	}


	public static class SourcePositionEx
	{
		public static SourcePosition ConvexHull(this IEnumerable<SourcePosition> self) => self.Aggregate(SourcePosition.ConvexHull);
		public static SourcePosition SourcePositionHull(this IEnumerable<INode> self) => self.Select(self => self.SourcePosition).ConvexHull();
		public static SourcePosition SourcePositionHull(this IEnumerable<IBoundExpression> self) => self.Select(self => self.OriginalNode).SourcePositionHull();
	}
}
