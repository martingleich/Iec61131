namespace Compiler.Scopes
{
	public sealed class RootStatementScope : AInnerScope<IScope>, IStatementScope
	{
		public RootStatementScope(IScope outerScope) : base(outerScope) { }
		public bool InsideLoop => false;
	}
}
