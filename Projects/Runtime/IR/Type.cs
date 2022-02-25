namespace Runtime.IR
{
	public readonly struct Type
	{
		public readonly int Size;
		public static readonly Type Bits0 = new(0);
		public static readonly Type Bits8 = new(1);
		public static readonly Type Bits16 = new(2);
		public static readonly Type Bits32 = new(4);
		public static readonly Type Bits64 = new(8);
		public static readonly Type Pointer = new(4);

		public Type(int size)
		{
			Size = size;
		}

		public override string ToString() => $"Bits{Size * 8}";
	}
}
