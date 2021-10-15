using Compiler.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Compiler
{
	public readonly struct OrderedSymbolSet<T> : IReadOnlyList<T> where T : ISymbol
	{
		public static readonly OrderedSymbolSet<T> Empty = new(ImmutableArray<T>.Empty);
		private readonly ImmutableArray<T> Values;

		private OrderedSymbolSet(ImmutableArray<T> values)
		{
			Values = values;
		}
		public T this[CaseInsensitiveString name] => GetValue(name);
		public T this[string name] => GetValue(name.ToCaseInsensitive());
		public T this[int index] => Values[index];
		public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Values).GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public bool TryGetValue(CaseInsensitiveString key, [MaybeNullWhen(false)] out T value)
		{
			foreach (var v in Values)
			{
				if (v.Name == key)
				{
					value = v;
					return true;
				}
			}
			value = default;
			return false;
		}
		public T GetValue(CaseInsensitiveString key)
		{
			if (TryGetValue(key, out var value))
				return value;
			else
				throw new ArgumentException($"Key {key} does not exist.", nameof(key));
		}
		public bool ContainsKey(CaseInsensitiveString key) => TryGetValue(key, out var _);
		public int Length => Values.Length;
		public int Count => Length;

		public static OrderedSymbolSet<T> Create(IEnumerable<T> symbols)
			=> CreateWithDuplicatesInternal(symbols, null);
		public static OrderedSymbolSet<T> CreateWithDuplicates(IEnumerable<T> symbols, MessageBag messages)
			=> CreateWithDuplicatesInternal(symbols, messages);
		private static OrderedSymbolSet<T> CreateWithDuplicatesInternal(IEnumerable<T> symbols, MessageBag? messages)
		{
			var existing = new Dictionary<CaseInsensitiveString, T>();
			var builder = ImmutableArray.CreateBuilder<T>();

			foreach (var sym in symbols)
			{
				if (existing.TryAdd(sym.Name, sym))
					builder.Add(sym);
				else
				{
					if(messages != null)
						messages.Add(new SymbolAlreadyExistsMessage(sym.Name, existing[sym.Name].DeclaringPosition, sym.DeclaringPosition));
					else
						throw new ArgumentException($"The symbol {sym} already exists.", nameof(symbols));
				}
			}
			return new(builder.ToImmutable());
		}
	}

	public static class OrderedSymbolSet
	{
		public static T? TryGetValue<T>(this OrderedSymbolSet<T> self, CaseInsensitiveString key) where T : class, ISymbol => self.TryGetValue(key, out var value) ? value : null;
		public static OrderedSymbolSet<T> ToOrderedSymbolSet<T>(params T[] symbols) where T : ISymbol
			=> symbols.AsEnumerable().ToOrderedSymbolSet();
		public static OrderedSymbolSet<T> ToOrderedSymbolSet<T>(this IEnumerable<T> symbols) where T : ISymbol
			=> OrderedSymbolSet<T>.Create(symbols);
		public static OrderedSymbolSet<TSymbol> ToOrderedSymbolSet<T, TSymbol>(this IEnumerable<T> values, Func<T, TSymbol> map) where TSymbol : ISymbol
			=> values.Select(map).ToOrderedSymbolSet();
		public static OrderedSymbolSet<T> ToOrderedSymbolSetWithDuplicates<T>(this IEnumerable<T> symbols, MessageBag messageBag) where T : ISymbol
			=> OrderedSymbolSet<T>.CreateWithDuplicates(symbols, messageBag);
		public static OrderedSymbolSet<TSymbol> ToOrderedSymbolSetWithDuplicates<T, TSymbol>(this IEnumerable<T> values, MessageBag messageBag, Func<T, TSymbol> map) where TSymbol : ISymbol
			=> values.Select(map).ToOrderedSymbolSetWithDuplicates(messageBag);
	}
}
