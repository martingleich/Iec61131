using System;

namespace StandardLibraryExtensions
{
	public static class StringExtensions
	{
		public static string ReplaceAt(this string self, int index, char c)
		{
			if (self == null)
				throw new ArgumentNullException(nameof(self));
			char[] chars = self.ToCharArray();
			chars[index] = c;
			return new string(chars);
		}
	}
}
