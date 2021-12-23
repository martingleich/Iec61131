using Compiler.Messages;

namespace Compiler.Scopes
{
	public sealed class TemporaryVariablesScope : AInnerScope<IScope>
	{
		public readonly OrderedSymbolSet<LocalVariableSymbol> LocalVariables;
		public TemporaryVariablesScope(IScope outerScope, OrderedSymbolSet<LocalVariableSymbol> localVariables) : base(outerScope)
		{
			LocalVariables = localVariables;
		}

		public override ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourceSpan sourceSpan)
		{
			if (LocalVariables.TryGetValue(identifier, out var localVariableSymbol))
				return localVariableSymbol;
			return base.LookupVariable(identifier, sourceSpan);
		}
	}
}
