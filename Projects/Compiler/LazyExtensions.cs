using System;

namespace Compiler
{
	public static class LazyExtensions
	{
		public static Lazy<T> Create<T>(Func<T> func) => new (func);
		public static Lazy<TResult> Select<T, TResult>(this Lazy<T> lazy, Func<T, TResult> map) => new (() => map(lazy.Value));
	}
}
