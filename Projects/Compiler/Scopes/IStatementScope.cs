namespace Compiler.Scopes
{
	public interface IStatementScope : IScope
	{
		public bool InsideLoop { get; }
	}
}
