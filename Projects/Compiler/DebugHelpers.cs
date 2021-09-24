using System;
using System.Collections.Generic;
using System.Linq;

namespace Compiler
{
	public static class DebugHelpers
	{
		/// <summary>
		/// Shuffles the enumerable randomly(but only if DEBUG is defined) does nothing otherwise. 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="input"></param>
		/// <returns></returns>
		public static IEnumerable<T> DebugShuffle<T>(this IEnumerable<T> input)
		{
			if (input is null)
				throw new ArgumentNullException(nameof(input));
#if DEBUG
			//return input.Shuffle(new Random(9));
			return input;
#else
			return input;
#endif
		}

		private static IEnumerable<T> Shuffle<T>(this IEnumerable<T> input, Random rnd)
		{
			// Fisher-Yates shuffle
			var list = input.ToArray();
			for (int i = list.Length - 1; i >= 0; --i)
			{
				int j = rnd.Next(i);
				yield return list[j];
				Swap(ref list[i], ref list[j]);
			}
		}
		private static void Swap<T>(ref T a, ref T b)
		{
			T tmp = a;
			a = b;
			b = tmp;
		}
	}
}
