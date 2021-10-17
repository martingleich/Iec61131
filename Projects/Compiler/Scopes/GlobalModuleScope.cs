using System;
using Compiler.Messages;
using Compiler.Types;

namespace Compiler.Scopes
{
	public sealed class GlobalModuleScope : AInnerScope<IScope>
	{
		private readonly BoundModuleInterface Interface;

		public GlobalModuleScope(BoundModuleInterface @interface, IScope inner) : base(inner)
		{
			Interface = @interface ?? throw new ArgumentNullException(nameof(@interface));
		}

		public override ErrorsAnd<ITypeSymbol> LookupType(CaseInsensitiveString identifier, SourcePosition sourcePosition) => Interface.DutTypes.TryGetValue(identifier, out var dutType)
			? ErrorsAnd.Create(dutType)
			: base.LookupType(identifier, sourcePosition);
		public override ErrorsAnd<GlobalVariableListSymbol> LookupGlobalVariableList(CaseInsensitiveString identifier, SourcePosition sourcePosition) => Interface.GlobalVariableListSymbols.TryGetValue(identifier, out var symbol)
			? ErrorsAnd.Create(symbol)
			: base.LookupGlobalVariableList(identifier, sourcePosition);
		public override ErrorsAnd<FunctionSymbol> LookupFunction(CaseInsensitiveString identifier, SourcePosition sourcePosition) => Interface.FunctionSymbols.TryGetValue(identifier, out var function)
			? ErrorsAnd.Create(function)
			: base.LookupFunction(identifier, sourcePosition);
	}
}
