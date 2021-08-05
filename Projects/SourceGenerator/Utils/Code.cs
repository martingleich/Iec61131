using System;

namespace SourceGenerator
{
	public class Code
	{
		public Code(string[] lines)
		{
			Lines = lines;
		}

		public string[] Lines { get; }
		public override string ToString() => string.Join(Environment.NewLine, Lines);
	}
}
