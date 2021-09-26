using Compiler.Messages;
using Compiler.Types;

namespace Compiler.Scopes
{
	public sealed class RootScope : IScope
	{
		public static readonly RootScope Instance = new();

		public SystemScope SystemScope { get; } = new SystemScope();

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
