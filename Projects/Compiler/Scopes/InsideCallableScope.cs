using System;
using Compiler.Messages;

namespace Compiler.Scopes
{
	public sealed class InsideCallableScope : AInnerScope<IScope>
	{
		private readonly ICallableSymbol Symbol;
		public InsideCallableScope(IScope outerScope, ICallableSymbol symbol) : base(outerScope)
		{
			Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
		}

		public override ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourcePosition sourcePosition)
		{
			if (Symbol.Parameters.TryGetValue(identifier, out var parameterSymbol))
				return parameterSymbol;
			return base.LookupVariable(identifier, sourcePosition);
		}
	}
}
