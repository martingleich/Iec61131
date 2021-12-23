using Compiler.Messages;

namespace Compiler.Scopes
{
	public sealed class InsideTypeScope : AInnerScope<IScope>
	{
		private readonly SymbolSet<FieldVariableSymbol> FieldVariables;
		public InsideTypeScope(IScope outerScope, SymbolSet<FieldVariableSymbol> fieldVariables) : base(outerScope)
		{
			FieldVariables = fieldVariables;
		}

		public override ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourceSpan sourceSpan)
		{
			if (FieldVariables.TryGetValue(identifier, out var localVariableSymbol))
				return localVariableSymbol;
			return base.LookupVariable(identifier, sourceSpan);
		}
	}
}
