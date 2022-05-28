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
		[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		public override string ToString() => $"{Area}:{Offset}";

		public static MemoryLocation operator +(MemoryLocation location, int offset) => new (location.Area, (ushort)(location.Offset + offset));
		public static MemoryLocation operator +(MemoryLocation location, LocalVarOffset offset) => location + offset.Offset;
		public static MemoryLocation operator -(MemoryLocation location, int offset) => location + (-offset);
	}
}
