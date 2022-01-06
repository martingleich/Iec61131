using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Compiler
{
	public static class EnumerableExtensions
	{
		public static bool TryGetSingle<T>(this ImmutableArray<T> self, [MaybeNullWhen(false)] out T singleValue)
		{
			if (self.Length == 1)
			{
				singleValue = self[0];
				return true;
			}
			else
			{
				singleValue = default;
				return false;
			}
		}

		public static bool HasNoNullElement<T>(this T?[] values, [MaybeNullWhen(false)] out T[] nonNulls)
		{
			if (values is null)
				throw new ArgumentNullException(nameof(values));
			foreach (var value in values)
			{
				if (value is null)
				{
					nonNulls = null;
					return false;
				}
			}
			nonNulls = values!;
			return true;
		}

		public static bool Equal<T>(ImmutableArray<T> r1, ImmutableArray<T> r2) where T:IEquatable<T>
		{
			if (r1.Length == r2.Length)
			{
				for (int i = 0; i < r1.Length; ++i)
				{
					if (!r1[i].Equals(r2[i]))
						return false;
				}
				return true;
			}
			else
			{
				return false;
			}
		}
		public static IEnumerable<T> Concat<T>(params IEnumerable<T>[] args)
		{
			foreach (var xs in args)
			{
				foreach (var x in xs)
					yield return x;
			}
		}

		public static void ShrinkToSize<T>(this List<T> self, int count)
		{
			self.RemoveRange(count, self.Count - count);
		}
	}
}
