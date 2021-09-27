using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Compiler
{
	public static class EnumerableExtensions
	{
		/// <summary>
		/// Try to retrieve the single value of the enumerable.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="self"></param>
		/// <param name="singleValue">If the sequence contains only a single element it is written here.</param>
		/// <returns>True if the sequence contains a single element, false otherwise</returns>
		public static bool TryGetSingle<T>(this IEnumerable<T> self, [MaybeNullWhen(false)] out T singleValue)
		{
			if (self == null)
				throw new ArgumentNullException(nameof(self));
			using (var e = self.GetEnumerator())
			{
				if (e.MoveNext())
				{
					singleValue = e.Current;
					if (!e.MoveNext())
					{
						return true;
					}
				}
			}
			singleValue = default;
			return false;
		}
	
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

		public static bool HasNoNullElement<T>(this T?[] values, [MaybeNullWhen(false)] out T[] nonNulls) where T : class
		{
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
	}
}
