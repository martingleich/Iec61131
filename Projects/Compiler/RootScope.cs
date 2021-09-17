using Compiler.Messages;
using Compiler.Types;

namespace Compiler
{
	public sealed class RootScope : IScope
	{
		public static readonly RootScope Instance = new();

		public EnumTypeSymbol? CurrentEnum => null;

		public SystemScope SystemScope { get; } = new SystemScope();

		public ErrorsAnd<ITypeSymbol> LookupType(CaseInsensitiveString identifier, SourcePosition sourcePosition)
			=> ErrorsAnd.Create(
				ITypeSymbol.CreateError(sourcePosition, identifier),
				new TypeNotFoundMessage(identifier.Original, sourcePosition));

		public ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourcePosition sourcePosition) =>
			ErrorsAnd.Create(
				IVariableSymbol.CreateError(sourcePosition, identifier),
				new VariableNotFoundMessage(identifier.Original, sourcePosition));
	}
}
