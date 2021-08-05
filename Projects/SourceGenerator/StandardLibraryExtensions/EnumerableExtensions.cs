#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace StandardLibraryExtensions
{
	public static class EnumerableExtensions
	{
		public static IEnumerable<T> Singleton<T>(T value)
		{
			yield return value;
		}
		public static IEnumerable<T> OptionalToEnumerable<T>(T? value) where T : class
			=> value != null ? Singleton(value) : Enumerable.Empty<T>();
		public static IEnumerable<T> OptionalToEnumerable<T>(T? value) where T : struct
			=> value.HasValue ? Singleton(value.Value) : Enumerable.Empty<T>();
		public static string DelimitWith<T>(this IEnumerable<T> self, string splitter)
		{
			if (splitter == null)
				throw new ArgumentNullException(nameof(splitter));
			return string.Join(splitter, self);
		}
		public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> self) where T : class
		{
			foreach (var x in self)
				if (x != null)
					yield return x;
		}
		public static IEnumerable<T> TakeWhileIncludingLast<T>(this IEnumerable<T> self, Func<T, bool> predicate)
		{
			if (self == null)
				throw new ArgumentNullException(nameof(self));
			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));
			return self.TakeWhileIncludingLastInternal(predicate);
		}
		private static IEnumerable<T> TakeWhileIncludingLastInternal<T>(this IEnumerable<T> self, Func<T, bool> predicate)
		{
			foreach(var el in self)
			{
				yield return el;
				if (!predicate(el))
					yield break;
			}
		}
		public static int MaxOrDefault<T>(this IEnumerable<T> self, Func<T, int> selector, int defaultValue = default)
		{
			int max = int.MinValue;
			bool any = false;
			foreach (var x in self)
			{
				any = true;
				var v = selector(x);
				if (v > max)
					max = v;
			}
			return any ? max : defaultValue;
		}
		public static int Xor<T>(this IEnumerable<T> self, Func<T, int> selector)
		{
			int result = 0;
			foreach (var x in self)
				result ^= selector(x);
			return result;
		}
		public static int Xor(this IEnumerable<int> self)
		{
			int result = 0;
			foreach (var x in self)
				result ^= x;
			return result;
		}

		/// <summary>
		/// True if the predicate holds for all sequence pairs.
		/// False otherwise or if the sequence have diffrent length.
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <typeparam name="T2"></typeparam>
		/// <param name="self"></param>
		/// <param name="other"></param>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public static bool IfAllPairs<T1, T2>(this IEnumerable<T1> self, IEnumerable<T2> other, Func<T1, T2, bool> predicate)
		{
			using (var e1 = self.GetEnumerator())
			using (var e2 = other.GetEnumerator())
			{
				var more1 = e1.MoveNext();
				var more2 = e2.MoveNext();
				if (!more1 && !more2)
					return true;
				if (more1 != more2)
					return false;
				do
				{
					if (!predicate(e1.Current, e2.Current))
						return false;
					more1 = e1.MoveNext();
					more2 = e2.MoveNext();
					if (more1 != more2)
						return false;
				} while (more1);
			}
			return true;
		}
	}
}
