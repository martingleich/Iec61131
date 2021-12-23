using Compiler.Messages;
using Compiler.Types;

namespace Compiler.Scopes
{
	public interface IScope
	{
		ErrorsAnd<ITypeSymbol> LookupType(CaseInsensitiveString identifier, SourceSpan sourceSpan);
		ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourceSpan sourceSpan);
		ErrorsAnd<IScopeSymbol> LookupScope(CaseInsensitiveString identifier, SourceSpan sourceSpan);

		SystemScope SystemScope { get; }
	}
}
