using System.Collections.Immutable;

namespace Compiler.Messages
{
	public static class ErrorsAnd
	{
		public static ErrorsAnd<T> Create<T>(T value, IMessage message) =>
			Create(value, ImmutableArray.Create(message));
		public static ErrorsAnd<T> Create<T>(T value) => Create(value, ImmutableArray<IMessage>.Empty);
		public static ErrorsAnd<T> Create<T>(T value, ImmutableArray<IMessage> messages) =>
			new(value, messages);
	}
}
