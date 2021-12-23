using Compiler.Messages;
using Compiler.Types;

namespace Compiler.Scopes
{
	public sealed class InsideEnumScope : AInnerScope<IScope>
	{
		private readonly EnumTypeSymbol EnumTypeSymbol;

		public InsideEnumScope(EnumTypeSymbol enumTypeSymbol, IScope outerScope) : base(outerScope)
		{
			EnumTypeSymbol = enumTypeSymbol;
		}

		public override ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourceSpan sourceSpan) =>
			EnumTypeSymbol.Values.TryGetValue(identifier, out var value) ? value : base.LookupVariable(identifier, sourceSpan);

		public override string ToString() => $"{EnumTypeSymbol} < {OuterScope}";
	}
}
