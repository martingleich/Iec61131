using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Compiler.Messages
{
	public sealed class MessageBag : ICollection<IMessage>
	{
		private readonly List<IMessage> Messages = new();

		public int Count => Messages.Count;
		public bool IsReadOnly => false;

		public void Add(IMessage message)
		{
			Messages.Add(message);
		}
		public void AddRange(IEnumerable<IMessage> messages)
		{
			Messages.AddRange(messages);
		}

		public ImmutableArray<IMessage> ToImmutable() => Messages.ToImmutableArray();

		public IEnumerator<IMessage> GetEnumerator() => Messages.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public void Clear() => Messages.Clear();
		public bool Contains(IMessage item) => Messages.Contains(item);
		public void CopyTo(IMessage[] array, int arrayIndex) => Messages.CopyTo(array, arrayIndex);
		public bool Remove(IMessage item) => Messages.Remove(item);
	}
}
