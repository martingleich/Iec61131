namespace Runtime.IR
{
	public sealed class NullExpression : IExpression
	{
		public static readonly NullExpression Instance = new();
		public void LoadTo(Runtime runtime, MemoryLocation location, int size) { }
		public override string ToString() => "null";
	}
}
