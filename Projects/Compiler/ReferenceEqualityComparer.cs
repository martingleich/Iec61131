using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Compiler
{
	public sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
	{
		public static readonly IEqualityComparer<T> Instance = new ReferenceEqualityComparer<T>();
		public bool Equals(T? x, T? y) => ReferenceEquals(x, y);
		public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
	}
}
