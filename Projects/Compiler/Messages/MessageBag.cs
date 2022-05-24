using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Compiler.Messages
{
	[DebuggerDisplay("Count = {Messages.Count}")]
	public sealed class MessageBag : IEnumerable<IMessage>
	{
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		private List<IMessage>? Messages;

		public MessageBag()
		{
		}
		public void Add(bool check, IMessage message)
		{
			if (check)
				Add(message);
		}
		public void Add(IMessage message)
		{
			if (Messages == null)
				Messages = new();
			Messages.Add(message);
		}
		public void AddRange(IEnumerable<IMessage> messages)
		{
			if (Messages == null)
				Messages = new();
			Messages.AddRange(messages);
		}

		public ImmutableArray<IMessage> ToImmutable()
		{
			if (Messages == null)
				return ImmutableArray<IMessage>.Empty;
			else
				return Messages.DebugShuffle().ToImmutableArray();
		}

		public IEnumerator<IMessage> GetEnumerator()
		{
			if (Messages == null)
				return EmptyEnumerator.Instance;
			else
				return Messages.DebugShuffle().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		private sealed class EmptyEnumerator : IEnumerator<IMessage>
		{
			public readonly static IEnumerator<IMessage> Instance = new EmptyEnumerator();
			public IMessage Current => throw new NotImplementedException();
			object IEnumerator.Current => throw new NotImplementedException();
			public void Dispose() {}
			public bool MoveNext() => false;
			public void Reset() { }
		}
	}
}
