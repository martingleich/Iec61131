using Compiler.CodegenIR;
using Runtime.IR.RuntimeTypes;

namespace Compiler.Types
{
    public interface IStructuredTypeSymbol : ITypeSymbol
	{
		SymbolSet<FieldVariableSymbol> Fields { get; }
    }
}