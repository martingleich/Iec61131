namespace Runtime.IR
{
	public readonly struct MemoryLocation
	{
		public readonly ushort Area;
		public readonly ushort Offset;

		public MemoryLocation(ushort area, ushort offset)
		{
			Area = area;
			Offset = offset;
		}
		public override string ToString() => $"{Area}:{Offset}";
	}
}
