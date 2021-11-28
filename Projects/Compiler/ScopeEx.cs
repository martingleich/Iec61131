using Compiler.Messages;
using Compiler.Scopes;

namespace Compiler
{
	public static class ScopeEx
	{
		public static ErrorsAnd<IScopeSymbol> ResolveScope(this IScope scope, ScopeQualifierSyntax syntax)
		{
			if (syntax.Scope is ScopeQualifierSyntax ownerScopeSyntax)
			{
				return scope.ResolveScope(ownerScopeSyntax).ApplyEx(ownerScope => ownerScope.LookupScope(syntax.ScopeName, syntax.TokenScopeName.SourcePosition));
			}
			else
			{
				return scope.LookupScope(syntax.ScopeName, syntax.TokenScopeName.SourcePosition);
			}
		}
	}
}
