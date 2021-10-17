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
		public SyntaxCommaSeparated(HeadSyntax? head, SourcePosition startPosition)
		{
			Head = head;
			SourcePosition = head is not null ? head.SourcePosition : startPosition;
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
				SourcePosition = SourcePosition.ConvexHull(FirstNonNullChild.SourcePosition, LastNonNullChild.SourcePosition);
			}
			public INode FirstNonNullChild => Value;
			public INode LastNonNullChild => (INode?)Tail ?? Value;
			public SourcePosition SourcePosition { get; }

			public IEnumerable<INode> GetChildren()
			{
				yield return Value;
				if(Tail is not null)
					yield return Tail;
			}
		}

		public readonly HeadSyntax? Head;
		public readonly SourcePosition SourcePosition { get; }

		public IEnumerator<T> GetEnumerator()
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
		public SyntaxArray(ImmutableArray<T> values, SourcePosition defaultStartPosition)
		{
			Values = values;
			if (values.Length > 0)
				SourcePosition = SourcePosition.ConvexHull(values[0].SourcePosition, values[^1].SourcePosition); // The convex hull is equal to the convex hull of the first and last element, because the elements are ordered by start position.
			else
				SourcePosition = defaultStartPosition;
		}

		public readonly ImmutableArray<T> Values;
		public SourcePosition SourcePosition { get; }

		public int Count => Values.Length;
		public T this[int index] => Values[index];
		public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Values).GetEnumerator();
		[ExcludeFromCodeCoverage]
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public IEnumerable<INode> GetChildren() => Values.CastArray<INode>();
	}

	public static class SyntaxArray
	{
		public static SyntaxArray<T> ToSyntaxArray<T>(this ImmutableArray<T> self, SourcePosition defaultStartPosition) where T : ISyntax
			=> new(self, defaultStartPosition);
		public static SyntaxArray<T> ToSyntaxArray<T>(this ImmutableArray<T>.Builder self, SourcePosition defaultStartPosition) where T : ISyntax
			=> self.ToImmutable().ToSyntaxArray(defaultStartPosition);
	}
	public static class StatementListSyntaxExt
	{
		public static StatementListSyntax ToStatementList(this ImmutableArray<IStatementSyntax> self, SourcePosition defaultStartPosition)
			=> new(self.ToSyntaxArray(defaultStartPosition));
		public static StatementListSyntax ToStatementList(this ImmutableArray<IStatementSyntax>.Builder self, SourcePosition defaultStartPosition)
			=> new(self.ToSyntaxArray(defaultStartPosition));
	}
}
