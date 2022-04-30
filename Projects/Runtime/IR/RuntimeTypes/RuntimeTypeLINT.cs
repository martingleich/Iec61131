namespace Runtime.IR.RuntimeTypes
{
    public sealed class RuntimeTypeLINT : IComparableRuntimeType
    {
        public static readonly RuntimeTypeLINT Instance = new();
        public string Name => "LINT";
        public string ReadValue(MemoryLocation location, Runtime runtime) => runtime.LoadLINT(location).ToString();
        public override string ToString() => Name;

        public int Compare(MemoryLocation a, MemoryLocation b, Runtime runtime) => runtime.LoadLINT(a).CompareTo(runtime.LoadLINT(b));
    }
}
