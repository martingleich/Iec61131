using System;
using Compiler.Messages;
using Compiler.Types;

namespace Compiler.Scopes
{
	public sealed class InsideCallableScope : AInnerScope<IScope>
	{
		private readonly ICallableTypeSymbol Symbol;
		public InsideCallableScope(IScope outerScope, ICallableTypeSymbol symbol) : base(outerScope)
		{
			Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
		}

		public override ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourceSpan sourceSpan)
		{
			if (Symbol.Parameters.TryGetValue(identifier, out var parameterSymbol))
				return parameterSymbol;
			return base.LookupVariable(identifier, sourceSpan);
		}
	}
}
