using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Compiler
{
	public interface ISyntax : INode
	{
	}

	public readonly struct SyntaxCommaSeparated<T> : ISyntax, IEnumerable<T> where T : INode
	{
		public SyntaxCommaSeparated(HeadSyntax? head, SourcePosition startPosition)
		{
			Head = head;
			SourcePosition = head?.SourcePosition ?? startPosition;
		}
		public class TailSyntax : ISyntax
		{
			public readonly CommaToken CommaToken;
			public readonly T Value;
			public readonly TailSyntax? Tail;
			public TailSyntax(CommaToken commaToken, T value, TailSyntax? tail)
			{
				CommaToken = commaToken;
				Value = value;
				Tail = tail;
				SourcePosition = SourcePosition.ConvexHull(FirstNonNullChild.SourcePosition, LastNonNullChild.SourcePosition);
			}
			public INode FirstNonNullChild => CommaToken;
			public INode LastNonNullChild => (INode?)Tail ?? Value;
			public SourcePosition SourcePosition { get; }
		}
		public class HeadSyntax : ISyntax
		{
			public readonly T Value;
			public readonly TailSyntax? Tail;
			public HeadSyntax(T value, TailSyntax? tail)
			{
				Value = value;
				Tail = tail;
				SourcePosition = SourcePosition.ConvexHull(FirstNonNullChild.SourcePosition, LastNonNullChild.SourcePosition);
			}
			public INode FirstNonNullChild => Value;
			public INode LastNonNullChild => (INode?)Tail ?? Value;
			public SourcePosition SourcePosition { get; }
		}

		public readonly HeadSyntax? Head;
		public readonly SourcePosition SourcePosition { get; }

		public IEnumerator<T> GetEnumerator()
		{
			if (Head != null)
			{
				yield return Head.Value;
				var tailParam = Head.Tail;
				while (tailParam != null)
				{
					yield return tailParam.Value;
					tailParam = tailParam.Tail;
				}
			}
		}

		[ExcludeFromCodeCoverage]
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	public readonly struct SyntaxArray<T> : ISyntax, IReadOnlyList<T> where T : ISyntax
	{
		public SyntaxArray(ImmutableArray<T> values, SourcePosition startPosition)
		{
			Values = values;
			if (values.Length > 0)
				SourcePosition = SourcePosition.ConvexHull(values[0].SourcePosition, values[^1].SourcePosition); // The convex hull is equal to the convex hull of the first and last element, because the elements are ordered by start position.
			else
				SourcePosition = startPosition;
		}

		public readonly ImmutableArray<T> Values;
		public SourcePosition SourcePosition { get; }

		public int Count => Values.Length;
		public T this[int index] => Values[index];
		public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Values).GetEnumerator();
		[ExcludeFromCodeCoverage]
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	public static class SyntaxArray
	{
		public static SyntaxArray<T> ToSyntaxArray<T>(this ImmutableArray<T> self, SourcePosition startPosition) where T : ISyntax
			=> new(self, startPosition);
		public static SyntaxArray<T> ToSyntaxArray<T>(this ImmutableArray<T>.Builder self, SourcePosition startPosition) where T : ISyntax
			=> self.ToImmutable().ToSyntaxArray(startPosition);
	}
}
