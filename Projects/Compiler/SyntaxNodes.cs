using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Compiler
{
	public interface ISyntax : INode
	{
		IEnumerable<INode> GetChildren();
	}

	public readonly struct SyntaxCommaSeparated<T> : ISyntax, IEnumerable<T> where T : INode
	{
		public SyntaxCommaSeparated(HeadSyntax? head, SourceSpan startPosition)
		{
			Head = head;
			SourceSpan = head is not null ? head.SourceSpan : startPosition;
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
				SourceSpan = SourceSpan.ConvexHull(FirstNonNullChild.SourceSpan, LastNonNullChild.SourceSpan);
			}
			public INode FirstNonNullChild => CommaToken;
			public INode LastNonNullChild => (INode?)Tail ?? Value;
			public SourceSpan SourceSpan { get; }

			public IEnumerable<INode> GetChildren()
			{
				yield return CommaToken;
				yield return Value;
				if (Tail is not null)
					yield return Tail;
			}
		}
		public class HeadSyntax : ISyntax
		{
			public readonly T Value;
			public readonly TailSyntax? Tail;
			public HeadSyntax(T value, TailSyntax? tail)
			{
				Value = value;
				Tail = tail;
				SourceSpan = SourceSpan.ConvexHull(FirstNonNullChild.SourceSpan, LastNonNullChild.SourceSpan);
			}
			public INode FirstNonNullChild => Value;
			public INode LastNonNullChild => (INode?)Tail ?? Value;
			public SourceSpan SourceSpan { get; }

			public IEnumerable<INode> GetChildren()
			{
				yield return Value;
				if(Tail is not null)
					yield return Tail;
			}
		}

		public readonly HeadSyntax? Head;
		public readonly SourceSpan SourceSpan { get; }

		public IEnumerable<T> Values
		{
			get
			{
				if (Head is not null)
				{
					yield return Head.Value;
					var tailParam = Head.Tail;
					while (tailParam is not null)
					{
						yield return tailParam.Value;
						tailParam = tailParam.Tail;
					}
				}
			}
		}
		public IEnumerator<T> GetEnumerator() => Values.GetEnumerator();

		[ExcludeFromCodeCoverage]
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public IEnumerable<INode> GetChildren()
		{
			if(Head is not null)
				yield return Head;
		}

		public int Count
		{
			get
			{
				if (Head is not null)
				{
					int c = 1;
					var tailParam = Head.Tail;
					while (tailParam is not null)
					{
						++c;
						tailParam = tailParam.Tail;
					}
					return c;
				}
				else
				{
					return 0;
				}
			}
		}
	}

	public readonly struct SyntaxArray<T> : ISyntax, IReadOnlyList<T> where T : ISyntax
	{
		public SyntaxArray(ImmutableArray<T> values, SourceSpan defaultStartPosition)
		{
			Values = values;
			if (values.Length > 0)
				SourceSpan = SourceSpan.ConvexHull(values[0].SourceSpan, values[^1].SourceSpan); // The convex hull is equal to the convex hull of the first and last element, because the elements are ordered by start span.
			else
				SourceSpan = defaultStartPosition;
		}

		public readonly ImmutableArray<T> Values;
		public SourceSpan SourceSpan { get; }

		public int Count => Values.Length;
		public T this[int index] => Values[index];
		public ImmutableArray<T>.Enumerator GetEnumerator() => Values.GetEnumerator();
		IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)Values).GetEnumerator();
		[ExcludeFromCodeCoverage]
		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Values).GetEnumerator();

		public IEnumerable<INode> GetChildren() => Values.CastArray<INode>();
	}

	public static class SyntaxArray
	{
		public static SyntaxArray<T> ToSyntaxArray<T>(this ImmutableArray<T> self, SourceSpan defaultStartPosition) where T : ISyntax
			=> new(self, defaultStartPosition);
		public static SyntaxArray<T> ToSyntaxArray<T>(this ImmutableArray<T>.Builder self, SourceSpan defaultStartPosition) where T : ISyntax
			=> self.ToImmutable().ToSyntaxArray(defaultStartPosition);
	}
	public static class StatementListSyntaxExt
	{
		public static StatementListSyntax ToStatementList(this ImmutableArray<IStatementSyntax> self, SourceSpan defaultStartPosition)
			=> new(self.ToSyntaxArray(defaultStartPosition));
		public static StatementListSyntax ToStatementList(this ImmutableArray<IStatementSyntax>.Builder self, SourceSpan defaultStartPosition)
			=> new(self.ToSyntaxArray(defaultStartPosition));
	}
}
