using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Compiler.Messages
{
	public readonly struct ErrorsAnd<T>
	{
		public readonly T Value;
		public readonly ImmutableArray<IMessage> Errors;

		public static implicit operator ErrorsAnd<T>(T value) => ErrorsAnd.Create(value);

		public ErrorsAnd(T value, ImmutableArray<IMessage> errors)
		{
			Value = value;
			Errors = errors;
		}

		public T Extract(MessageBag messages) => Extract(messages, out _);
		public T Extract(MessageBag messages, out bool hasError)
		{
			messages.AddRange(Errors);
			hasError = Errors.Length != 0;
			return Value;
		}

		public bool HasErrors => Errors.Any();

		public ErrorsAnd<T2> MoveErrors<T2>(T2 newValue) => new(newValue, Errors);
		public ErrorsAnd<T2> Select<T2>(Func<T, T2> func) => new(func(Value), Errors);
		public ErrorsAnd<T2> ApplyEx<T2>(Func<T, ErrorsAnd<T2>> func)
		{
			var result = func(Value);
			return new ErrorsAnd<T2>(result.Value, Errors.AddRange(result.Errors));
		}

		public ErrorsAnd<T2> Cast<T2>() where T2 : class => new((Value as T2)!, Errors);

		[ExcludeFromCodeCoverage]
		public override string? ToString() => Value?.ToString();
	}
}
