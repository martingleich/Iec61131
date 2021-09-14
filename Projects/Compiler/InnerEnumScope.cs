using Compiler.Messages;

namespace Compiler
{
	public sealed class InnerEnumScope : AInnerScope
	{
		private readonly EnumTypeSymbol EnumTypeSymbol;

		public InnerEnumScope(EnumTypeSymbol enumTypeSymbol, IScope outerScope) : base(outerScope)
		{
			EnumTypeSymbol = enumTypeSymbol;
		}

		public override EnumTypeSymbol? CurrentEnum => EnumTypeSymbol;

		public override ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourcePosition sourcePosition) =>
			EnumTypeSymbol.Values.TryGetValue(identifier, out var value) ? value : base.LookupVariable(identifier, sourcePosition);

		public override string ToString() => $"{EnumTypeSymbol} < {OuterScope}";
	}
}
