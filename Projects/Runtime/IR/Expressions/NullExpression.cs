namespace Runtime.IR.Expressions
{
	public sealed class NullExpression : IExpression
	{
		public static readonly NullExpression Instance = new();
		public void LoadTo(RTE runtime, MemoryLocation location, int size) { }
		public override string ToString() => "null";
	}
}
