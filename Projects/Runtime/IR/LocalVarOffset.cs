namespace Runtime.IR
{
	public readonly struct LocalVarOffset
	{
		public readonly ushort Offset;

		public LocalVarOffset(ushort offset)
		{
			Offset = offset;
		}

		public override string ToString() => $"stack{Offset}";
	}
}
