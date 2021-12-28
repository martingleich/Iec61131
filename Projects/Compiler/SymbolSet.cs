using Compiler.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Compiler
{
	[DebuggerDisplay("Count = {Count}")]
	public readonly struct SymbolSet<T> : IEnumerable<T> where T : ISymbol
	{
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly ImmutableDictionary<CaseInsensitiveString, T> Values;

		public static readonly SymbolSet<T> Empty = new(ImmutableDictionary<CaseInsensitiveString, T>.Empty);

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public IEnumerable<CaseInsensitiveString> Keys => Values.Keys;

		public int Count => Values.Count;

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable IDE1006 // Naming Styles
		private T[] _debugDisplayValues => Values.Values.ToArray(); 
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore IDE0051 // Remove unused private members

		private SymbolSet(ImmutableDictionary<CaseInsensitiveString, T> values)
		{
			Values = values ?? throw new ArgumentNullException(nameof(values));
		}

		public T this[CaseInsensitiveString key] => Values[key];
		public T this[string key] => Values[key.ToCaseInsensitive()];
		public bool TryGetValue(CaseInsensitiveString key, [NotNullWhen(true)] out T? value) => Values.TryGetValue(key, out value);
		public IEnumerator<T> GetEnumerator() => Values.Values.DebugShuffle().GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public bool IsDefault => Values == null;

		public static SymbolSet<T> Create(IEnumerable<T> symbols)
			=> CreateWithDuplicatesInternal(symbols, null);
		public static SymbolSet<T> CreateWithDuplicates(IEnumerable<T> symbols, MessageBag messages)
			=> CreateWithDuplicatesInternal(symbols, messages);
		public static SymbolSet<T> Create<TSource>(IEnumerable<TSource> symbols, Func<TSource, T> map)
			=> CreateWithDuplicatesInternal(symbols, map, null);
		public static SymbolSet<T> CreateWithDuplicates<TSource>(IEnumerable<TSource> symbols, Func<TSource, T> map, MessageBag messages)
			=> CreateWithDuplicatesInternal(symbols, map, messages);
		private static SymbolSet<T> CreateWithDuplicatesInternal(IEnumerable<T> symbols, MessageBag? messages)
		{
			var builder = ImmutableDictionary.CreateBuilder<CaseInsensitiveString, T>();
			foreach (var sym in symbols)
			{
				if (!builder.TryAdd(sym.Name, sym))
				{
					if(messages != null)
						messages.Add(new SymbolAlreadyExistsMessage(sym.Name, builder[sym.Name].DeclaringSpan, sym.DeclaringSpan));
					else
						throw new ArgumentException($"The symbol {sym} already exists.", nameof(symbols));
				}
			}
			return new (builder.ToImmutable());
		}
		private static SymbolSet<T> CreateWithDuplicatesInternal<TSource>(IEnumerable<TSource> source, Func<TSource, T> map, MessageBag? messages)
		{
			var builder = ImmutableDictionary.CreateBuilder<CaseInsensitiveString, T>();
			foreach (var x in source)
			{
				var sym = map(x);
				if (!builder.TryAdd(sym.Name, sym))
				{
					if(messages != null)
						messages.Add(new SymbolAlreadyExistsMessage(sym.Name, builder[sym.Name].DeclaringSpan, sym.DeclaringSpan));
					else
						throw new ArgumentException($"The symbol {x} already exists.", nameof(source));
				}
			}
			return new (builder.ToImmutable());
		}
	}

	public static class SymbolSet
	{
		public static T? TryGetValue<T>(this SymbolSet<T> self, CaseInsensitiveString key) where T : class, ISymbol => self.TryGetValue(key, out var value) ? value : null;
		public static SymbolSet<T> Create<T>(params T[] symbols) where T : ISymbol
			=> SymbolSet<T>.Create(symbols);
		public static SymbolSet<T> ToSymbolSet<T>(this IEnumerable<T> allSymbols) where T : ISymbol
			=> SymbolSet<T>.Create(allSymbols);
		public static SymbolSet<TSymbol> ToSymbolSet<T, TSymbol>(this IEnumerable<T> allValues, Func<T, TSymbol> map) where TSymbol : ISymbol
			=> SymbolSet<TSymbol>.Create(allValues, map);
		public static SymbolSet<T> ToSymbolSetWithDuplicates<T>(this IEnumerable<T> allSymbols, MessageBag messageBag) where T : ISymbol
			=> SymbolSet<T>.CreateWithDuplicates(allSymbols, messageBag);
		public static SymbolSet<TSymbol> ToSymbolSetWithDuplicates<T, TSymbol>(this IEnumerable<T> allValues, MessageBag messageBag, Func<T, TSymbol> map) where TSymbol : ISymbol
			=> SymbolSet<TSymbol>.CreateWithDuplicates(allValues, map, messageBag);
	}
}
