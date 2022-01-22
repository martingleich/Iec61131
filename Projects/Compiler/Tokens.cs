namespace Compiler
{
	public interface INode
	{
		SourceSpan SourceSpan { get; }
	}
	public interface IToken : INode
	{
		SourcePoint StartPosition { get; }
		int Length { get; }
		string? Generating { get; }
		IToken? LeadingNonSyntax { get; }
		IToken? TrailingNonSyntax { get; set; }
	}
	public interface ITokenWithValue<out T> : IToken
	{
		T Value { get; }
	}
}
