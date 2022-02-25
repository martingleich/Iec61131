namespace Runtime.IR
{
	public sealed class Return : IStatement
	{
		public static readonly Return Instance = new();
		public int? Execute(Runtime runtime) => runtime.Return();

		public override string ToString() => "    return";
	}
}
