using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Compiler;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace FullEditor
{
	public static class SyntaxTreeUtils
	{
		public sealed class SortedNodes<T> : IEnumerable<T> where T : IToken
		{
			private readonly INode[] Roots;
			//public readonly SourceSpan? Span;
			//public readonly bool IncludeTrivia;
			public readonly bool IncludeError;

			public SortedNodes(INode[] roots, bool includeError)
			{
				Roots = roots ?? throw new ArgumentNullException(nameof(roots));
				IncludeError = includeError;
			}

			public IEnumerator<T> GetEnumerator() => new Enumerator(this);
			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
			private sealed class Enumerator : IEnumerator<T>
			{
				private Stack<IEnumerator<INode>>? VisitorStack = null;
				private readonly SortedNodes<T> Owner;
				[MaybeNull]
				private T _Current;

				public Enumerator(SortedNodes<T> owner)
				{
					Owner = owner;
				}
				public T Current => _Current ?? throw new InvalidOperationException();
				object IEnumerator.Current => Current;

				public bool MoveNext()
				{
					while (MoveNextInternal(out var node))
					{
						if (Owner.IncludeError || node.Generating != null)
						{
							_Current = node;
							return true;
						}
					}
					return false;
				}
				private bool MoveNextInternal([NotNullWhenAttribute(true)] out T? node)
				{
					if (VisitorStack == null)
					{
						VisitorStack = new Stack<IEnumerator<INode>>();
						VisitorStack.Push(((IEnumerable<INode>)Owner.Roots).GetEnumerator());
					}

					while (VisitorStack.Count > 0)
					{
						var top = VisitorStack.Peek();
						if (top.MoveNext())
						{
							if (top.Current is ISyntax syntax)
							{
								VisitorStack.Push(syntax.GetChildren().GetEnumerator());
							}
							else if (top.Current is T result)
							{
								node = result;
								return true;
							}
						}
						else
						{
							VisitorStack.Pop();
						}
					}
					node = default;
					return false;
				}

				public void Dispose() { }
				public void Reset()
				{
					throw new NotImplementedException();
				}
			}

			public SortedNodes<TResult> OfType<TResult>() where TResult : IToken => new(Roots, IncludeError);
		}

		public static SortedNodes<IToken> GetAllTokens(this ISyntax node, bool includeError = false) => new(new[] { node }, includeError);
		public static SortedNodes<IToken> GetAllTokens(IEnumerable<ISyntax> nodes, bool includeError = false) => new(nodes.OrderBy(x => x.SourceSpan.Start).ToArray(), includeError);
	}
}
