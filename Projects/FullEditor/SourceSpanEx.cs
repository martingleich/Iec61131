using SyntaxEditor;

namespace FullEditor
{
	public static class SourceSpanEx
	{
		public static IntSpan ToOffsetSpan(this Compiler.SourceSpan span) => IntSpan.FromStartLength(span.Start.Offset, span.Length);
	}
}
