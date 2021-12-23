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

		public ErrorsAnd<IScopeSymbol> LookupScope(CaseInsensitiveString identifier, SourceSpan sourceSpan)
			=> EmptyScopeHelper.LookupScope(CaseInsensitiveString.Empty, identifier, sourceSpan);
		public ErrorsAnd<ITypeSymbol> LookupType(CaseInsensitiveString identifier, SourceSpan sourceSpan)
			=> EmptyScopeHelper.LookupType(CaseInsensitiveString.Empty, identifier, sourceSpan);
		public ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourceSpan sourceSpan)
			=> EmptyScopeHelper.LookupVariable(CaseInsensitiveString.Empty, identifier, sourceSpan);
	}
}
