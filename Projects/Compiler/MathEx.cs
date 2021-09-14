using System;

namespace Compiler
{
	public static class MathEx
	{
		public static int Lcm(int a, int b)
		{
			if (a <= 0)
				throw new ArgumentException($"a({a}) must be non-negative", nameof(a));
			if (b <= 0)
				throw new ArgumentException($"b({b}) must be non-negative", nameof(b));
			return checked((a / Gcd(a, b)) * b);
		}

		public static int Gcd(int a, int b)
		{
			if (a <= 0)
				throw new ArgumentException($"a({a}) must be non-negative", nameof(a));
			if (b <= 0)
				throw new ArgumentException($"b({b}) must be non-negative", nameof(b));
			while (a != 0 && b != 0)
			{
				if (a > b)
					a %= b;
				else
					b %= a;
			}

			return a | b;
		}
	}
}
