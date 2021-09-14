using Compiler.Messages;
using Compiler.Types;

namespace Compiler
{
	public abstract class AInnerScope : IScope
	{
		protected readonly IScope OuterScope;

		protected AInnerScope(IScope outerScope)
		{
			OuterScope = outerScope;
		}

		public virtual EnumTypeSymbol? CurrentEnum => OuterScope.CurrentEnum;
		public virtual ErrorsAnd<ITypeSymbol> LookupType(CaseInsensitiveString identifier, SourcePosition sourcePosition) => OuterScope.LookupType(identifier, sourcePosition);
		public virtual ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourcePosition sourcePosition) => OuterScope.LookupVariable(identifier, sourcePosition);
	}
}
