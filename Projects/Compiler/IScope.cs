using Compiler.Messages;

namespace Compiler
{
	public interface IScope
	{
		ErrorsAnd<ITypeSymbol> LookupType(CaseInsensitiveString identifier, SourcePosition sourcePosition);
		ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourcePosition sourcePosition);

		EnumTypeSymbol? CurrentEnum { get; }
	}
}
