namespace Runtime.IR
{
	public sealed class LoadValueExpression : IExpression
	{
		public readonly LocalVarOffset Offset;

		public LoadValueExpression(LocalVarOffset offset)
		{
			Offset = offset;
		}

		public void LoadTo(Runtime runtime, MemoryLocation location, int size)
		{
			var pointer = runtime.LoadEffectiveAddress(Offset);
			runtime.Copy(pointer, location, size);
		}
		public override string ToString() => $"{Offset}";
	}
}
