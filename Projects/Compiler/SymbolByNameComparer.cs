using System.Collections.Generic;

namespace Compiler
{
	public sealed class SymbolByNameComparer<T> : IEqualityComparer<T> where T : ISymbol
	{
		public static readonly SymbolByNameComparer<T> Instance = new();
		public bool Equals(T? x, T? y) => ReferenceEquals(x, y) || (x is not null && y is not null && x.Name == y.Name);
		public int GetHashCode(T obj) => obj.Name.GetHashCode();
	}
}