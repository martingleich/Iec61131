using Compiler.Messages;
using Compiler.Types;

namespace Compiler
{
	public interface IScope
	{
		ErrorsAnd<ITypeSymbol> LookupType(CaseInsensitiveString identifier, SourcePosition sourcePosition);
		ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourcePosition sourcePosition);

		EnumTypeSymbol? CurrentEnum { get; }
		SystemScope SystemScope { get; }
	}
}
