namespace Compiler.Types
{
	public interface ITypeSymbol : ISymbol, IType
	{
		public static ITypeSymbol CreateError(SourcePosition declaringPosition, CaseInsensitiveString name) => new ErrorTypeSymbol(declaringPosition, name);
	}
}