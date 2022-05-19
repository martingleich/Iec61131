namespace Runtime.IR.RuntimeTypes
{
    public sealed class RuntimeTypeSINT : IComparableRuntimeType
    {
        public static readonly RuntimeTypeSINT Instance = new();
        public string Name => "SINT";
        public string ReadValue(MemoryLocation location, Runtime runtime) => runtime.LoadSINT(location).ToString();
        public override string ToString() => Name;

        public int Compare(MemoryLocation a, MemoryLocation b, Runtime runtime) => runtime.LoadSINT(a).CompareTo(runtime.LoadSINT(b));
    }
}
