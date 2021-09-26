namespace Compiler.Scopes
{
	public sealed class LoopScope : AInnerScope<IStatementScope>, IStatementScope
	{
		public LoopScope(IStatementScope outerScope) : base(outerScope)
		{
		}
		public bool InsideLoop => true;
	}
}
