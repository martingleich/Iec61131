namespace Compiler.Types
{
	public interface ITypeSymbol : ISymbol, IType
	{
		public static ITypeSymbol CreateError(SourcePosition declaringPosition, CaseInsensitiveString typeName) => new ErrorTypeSymbol(declaringPosition, typeName);
		public static ITypeSymbol CreateErrorForVar(SourcePosition declaringPosition, CaseInsensitiveString varName) => new ErrorTypeSymbol(declaringPosition, $"typeof[{varName}]".ToCaseInsensitive());
	}
}