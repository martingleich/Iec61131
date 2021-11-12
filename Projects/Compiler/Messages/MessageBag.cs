using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Compiler.Messages
{
	[DebuggerDisplay("Count = {Count}")]
	public sealed class MessageBag : ICollection<IMessage>
	{
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		private readonly List<IMessage> Messages = new();

		public int Count => Messages.Count;
		public bool IsReadOnly => false;

		public void Add(bool check, IMessage message)
		{
			if (check)
				Add(message);
		}
		public void Add(IMessage message)
		{
			Messages.Add(message);
		}
		public void AddRange(IEnumerable<IMessage> messages)
		{
			Messages.AddRange(messages);
		}

		public ImmutableArray<IMessage> ToImmutable() => Messages.DebugShuffle().ToImmutableArray();

		public IEnumerator<IMessage> GetEnumerator() => Messages.DebugShuffle().GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public void Clear() => Messages.Clear();
		public bool Contains(IMessage item) => Messages.Contains(item);
		public void CopyTo(IMessage[] array, int arrayIndex)
		{
			Messages.CopyTo(array, arrayIndex);
		}
		public bool Remove(IMessage item) => Messages.Remove(item);
	}
}
