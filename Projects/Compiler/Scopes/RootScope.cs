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
			=> ErrorsAnd.Create(
				IScopeSymbol.CreateError(identifier, sourcePosition),
				new ScopeNotFoundMessage(identifier, sourcePosition));

		public ErrorsAnd<ITypeSymbol> LookupType(CaseInsensitiveString identifier, SourcePosition sourcePosition)
			=> ErrorsAnd.Create(
				ITypeSymbol.CreateError(sourcePosition, identifier),
				new TypeNotFoundMessage(identifier, sourcePosition));

		public ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourcePosition sourcePosition) =>
			ErrorsAnd.Create(
				IVariableSymbol.CreateError(sourcePosition, identifier),
				new VariableNotFoundMessage(identifier, sourcePosition));
	}
}
