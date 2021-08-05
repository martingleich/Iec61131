using System.Collections.Generic;

namespace SourceGenerator
{
	public class CodeWriter
	{
		private readonly List<string> Lines = new();
		private int CurIndentation;
		public void Indent()
		{
			CurIndentation++;
		}
		public void Unindent()
		{
			CurIndentation--;
		}
		private string IndentString => new('\t', CurIndentation);

		public void StartBlock()
		{
			WriteLine("{");
			Indent();
		}
		public void EndBlock(string terminator = "")
		{
			Unindent();
			WriteLine("}" + terminator);
		}
		public void WriteLines(IEnumerable<string> lines)
		{
			foreach (var line in lines)
				WriteLine(line);
		}
		public void WriteLine()
			=> WriteLine("");
		public void WriteLine(string str)
		{
			if (string.IsNullOrWhiteSpace(str))
				Lines.Add("");
			else
				Lines.Add(IndentString + str);
		}

		public void WriteCode(Code code)
		{
			foreach (var line in code.Lines)
				WriteLine(line);
		}

		public Code ToCode() => new(Lines.ToArray());
		public override string ToString() => ToCode().ToString();
	}
}
