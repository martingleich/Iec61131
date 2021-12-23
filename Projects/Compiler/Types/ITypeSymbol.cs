namespace Compiler.Types
{
	public interface ITypeSymbol : ISymbol, IType
	{
		public UniqueSymbolId UniqueId { get; }
		public static ITypeSymbol CreateError(SourceSpan declaringSpan, CaseInsensitiveString typeName) => new ErrorTypeSymbol(declaringSpan, typeName);
		public static ITypeSymbol CreateErrorForVar(SourceSpan declaringSpan, CaseInsensitiveString varName) => new ErrorTypeSymbol(declaringSpan, $"typeof[{varName}]".ToCaseInsensitive());
		public static ITypeSymbol CreateErrorForFunc(SourceSpan declaringSpan, CaseInsensitiveString funcName) => new ErrorTypeSymbol(declaringSpan, $"typeof[{funcName}]".ToCaseInsensitive());
	}
}