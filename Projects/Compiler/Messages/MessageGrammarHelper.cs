using System.Collections.Generic;
using System.Text;

namespace Compiler.Messages
{
	public static class MessageGrammarHelper
	{
		public static string OrListing<T>(IEnumerable<T> values) where T : notnull
			=> Listing(values, ", ", " or ");
		public static string AndListing<T>(IEnumerable<T> values) where T : notnull
			=> Listing(values, ", ", " and ");

		private static string Listing<T>(IEnumerable<T> values, string sep, string terminator) where T : notnull
		{
			using var e = values.GetEnumerator();
			var sb = new StringBuilder();
			if (e.MoveNext())
			{
				sb.Append(e.Current);
				bool hasMore = e.MoveNext();
				while (hasMore)
				{
					var value = e.Current;
					hasMore = e.MoveNext();
					if (hasMore)
						sb.Append(sep);
					else
						sb.Append(terminator);
					sb.Append(value);
				}
			}
			return sb.ToString();
		}
	}
}
