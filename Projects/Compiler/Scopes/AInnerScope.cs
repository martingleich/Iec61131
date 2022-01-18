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
		public virtual ErrorsAnd<ITypeSymbol> LookupType(CaseInsensitiveString identifier, SourceSpan sourceSpan) => OuterScope.LookupType(identifier, sourceSpan);
		public virtual ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourceSpan sourceSpan) => OuterScope.LookupVariable(identifier, sourceSpan);
		public virtual ErrorsAnd<IScopeSymbol> LookupScope(CaseInsensitiveString identifier, SourceSpan sourceSpan) => OuterScope.LookupScope(identifier, sourceSpan);
	}
	
	public abstract class AInnerStatementScope<TScope> : AInnerScope<IStatementScope>, IStatementScope where TScope : IStatementScope
	{
		protected AInnerStatementScope(TScope outerScope) : base(outerScope) {}

		public bool InsideLoop => OuterScope.InsideLoop;
	}
}
