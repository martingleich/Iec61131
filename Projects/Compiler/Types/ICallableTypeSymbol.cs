namespace Compiler.Types
{
	public interface ICallableTypeSymbol : ITypeSymbol
	{
		OrderedSymbolSet<ParameterVariableSymbol> Parameters { get; }
	}
}