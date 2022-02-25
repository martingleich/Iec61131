namespace Runtime.IR
{
	public sealed class DerefExpression : IExpression
	{
		public readonly LocalVarOffset Address;

		public DerefExpression(LocalVarOffset location)
		{
			Address = location;
		}

		public void LoadTo(Runtime runtime, MemoryLocation location, int size)
		{
			var l1 = runtime.LoadPointer(Address);
			runtime.Copy(l1, location, size);
		}

		public override string ToString() => $"*{Address}";
	}
}
