using System;
using Compiler.Messages;

namespace Compiler.Scopes
{
	public sealed class InsideFunctionScope : AInnerScope<IScope>, IStatementScope
	{
		private readonly FunctionSymbol Symbol;
		private readonly SymbolSet<LocalVariableSymbol> LocalVariables;
		public InsideFunctionScope(IScope outerScope, FunctionSymbol symbol, SymbolSet<LocalVariableSymbol> localVariables) : base(outerScope)
		{
			Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
			LocalVariables = localVariables;
		}

		public bool InsideLoop => false;

		public override ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourcePosition sourcePosition)
		{
			if (LocalVariables.TryGetValue(identifier, out var localVariableSymbol))
				return localVariableSymbol;
			if (Symbol.Parameters.TryGetValue(identifier, out var parameterSymbol))
				return parameterSymbol;
			return base.LookupVariable(identifier, sourcePosition);
		}
	}
}
