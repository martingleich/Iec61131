namespace Compiler.Types
{
	public interface ICallableTypeSymbol : ITypeSymbol
	{
		public OrderedSymbolSet<ParameterVariableSymbol> Parameters { get; }
	}
	
}