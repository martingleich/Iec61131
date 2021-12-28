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
			return input.BiasedShuffle(465346542);
#else
			return input;
#endif
		}

		// Biased sampling is faster and good enough for this use case.
		private static uint XorShift_Next(uint state)
		{
			state ^= state << 13;
			state ^= state >> 13;
			state ^= state << 5;
			return state;
		}
		private static int BiasedIntInRange(uint state, int maxValue) => (int)(state % maxValue);
		private static IEnumerable<T> BiasedShuffle<T>(this IEnumerable<T> input, uint seed)
		{
			// Fisher-Yates shuffle
			var list = input.ToArray();
			uint state = seed;
			for (int i = 0; i < list.Length; ++i)
			{
				state = XorShift_Next(state);
				int j = i + BiasedIntInRange(state, list.Length - i);
				yield return list[j];
				list[j] = list[i];
			}
		}
	}
}
