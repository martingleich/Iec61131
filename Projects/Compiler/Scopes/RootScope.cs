using Compiler.Messages;
using Compiler.Types;
using System;

namespace Compiler.Scopes
{
	public sealed class RootScope : IScope
	{
		public RootScope(SystemScope systemScope)
		{
			SystemScope = systemScope ?? throw new ArgumentNullException(nameof(systemScope));
		}

		public SystemScope SystemScope { get; }

		public ErrorsAnd<IScopeSymbol> LookupScope(CaseInsensitiveString identifier, SourcePosition sourcePosition)
			=> EmptyScopeHelper.LookupScope(CaseInsensitiveString.Empty, identifier, sourcePosition);
		public ErrorsAnd<ITypeSymbol> LookupType(CaseInsensitiveString identifier, SourcePosition sourcePosition)
			=> EmptyScopeHelper.LookupType(CaseInsensitiveString.Empty, identifier, sourcePosition);
		public ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourcePosition sourcePosition)
			=> EmptyScopeHelper.LookupVariable(CaseInsensitiveString.Empty, identifier, sourcePosition);
	}
}
