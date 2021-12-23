using System;
using Compiler.Messages;
using Compiler.Types;

namespace Compiler.Scopes
{
	public sealed class GlobalInternalModuleScope : AInnerScope<IScope>
	{
		private readonly BoundModuleInterface Interface;

		public GlobalInternalModuleScope(BoundModuleInterface @interface, IScope outer) : base(outer)
		{
			Interface = @interface ?? throw new ArgumentNullException(nameof(@interface));
		}

		public override ErrorsAnd<ITypeSymbol> LookupType(CaseInsensitiveString identifier, SourceSpan sourceSpan) => Interface.Types.TryGetValue(identifier, out var dutType)
			? ErrorsAnd.Create(dutType)
			: base.LookupType(identifier, sourceSpan);
		public override ErrorsAnd<IScopeSymbol> LookupScope(CaseInsensitiveString identifier, SourceSpan sourceSpan) => Interface.Scopes.TryGetValue(identifier, out var symbol)
			? ErrorsAnd.Create(symbol)
			: base.LookupScope(identifier, sourceSpan);
		public override ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourceSpan sourceSpan) => Interface.FunctionSymbols.TryGetValue(identifier, out var functionVariable)
			? ErrorsAnd.Create<IVariableSymbol>(functionVariable)
			: base.LookupVariable(identifier, sourceSpan);
	}
}
