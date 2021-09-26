﻿using Compiler.Messages;
using Compiler.Types;

namespace Compiler.Scopes
{
	public sealed class InnerEnumScope : AInnerScope<IScope>
	{
		private readonly EnumTypeSymbol EnumTypeSymbol;

		public InnerEnumScope(EnumTypeSymbol enumTypeSymbol, IScope outerScope) : base(outerScope)
		{
			EnumTypeSymbol = enumTypeSymbol;
		}

		public override ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourcePosition sourcePosition) =>
			EnumTypeSymbol.Values.TryGetValue(identifier, out var value) ? value : base.LookupVariable(identifier, sourcePosition);

		public override string ToString() => $"{EnumTypeSymbol} < {OuterScope}";
	}
}
