using Compiler.Messages;
using Compiler.Types;

namespace Compiler.Scopes
{
	public abstract class AInnerScope<TScope> : IScope where TScope : IScope
	{
		protected readonly TScope OuterScope;

		protected AInnerScope(TScope outerScope)
		{
			OuterScope = outerScope;
		}

		public SystemScope SystemScope => OuterScope.SystemScope;
		public virtual ErrorsAnd<ITypeSymbol> LookupType(CaseInsensitiveString identifier, SourcePosition sourcePosition) => OuterScope.LookupType(identifier, sourcePosition);
		public virtual ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourcePosition sourcePosition) => OuterScope.LookupVariable(identifier, sourcePosition);
		public virtual ErrorsAnd<IScopeSymbol> LookupScope(CaseInsensitiveString identifier, SourcePosition sourcePosition) => OuterScope.LookupScope(identifier, sourcePosition);
	}
}
