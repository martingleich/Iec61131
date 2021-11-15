﻿using Compiler.Messages;
using Compiler.Types;

namespace Compiler.Scopes
{
	public interface IScope
	{
		ErrorsAnd<ITypeSymbol> LookupType(CaseInsensitiveString identifier, SourcePosition sourcePosition);
		ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourcePosition sourcePosition);
		ErrorsAnd<IScopeSymbol> LookupScope(CaseInsensitiveString identifier, SourcePosition sourcePosition);

		SystemScope SystemScope { get; }
	}
}
