using Compiler.Messages;

namespace Compiler.Scopes
{
	public sealed class TemporaryVariablesScope : AInnerScope<IScope>
	{
		private readonly SymbolSet<LocalVariableSymbol> LocalVariables;
		public TemporaryVariablesScope(IScope outerScope, SymbolSet<LocalVariableSymbol> localVariables) : base(outerScope)
		{
			LocalVariables = localVariables;
		}

		public override ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourcePosition sourcePosition)
		{
			if (LocalVariables.TryGetValue(identifier, out var localVariableSymbol))
				return localVariableSymbol;
			return base.LookupVariable(identifier, sourcePosition);
		}
	}
}
