namespace Compiler.Types
{
    public interface IStructuredTypeSymbol : ITypeSymbol
	{
		SymbolSet<FieldVariableSymbol> Fields { get; }
	}
}