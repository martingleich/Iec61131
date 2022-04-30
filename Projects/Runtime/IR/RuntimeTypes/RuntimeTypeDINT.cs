namespace Runtime.IR.RuntimeTypes
{
    public sealed class RuntimeTypeDINT : IComparableRuntimeType
    {
        public static readonly RuntimeTypeDINT Instance = new();
        public string Name => "DINT";
        public string ReadValue(MemoryLocation location, Runtime runtime) => runtime.LoadDINT(location).ToString();
        public override string ToString() => Name;

        public int Compare(MemoryLocation a, MemoryLocation b, Runtime runtime) => runtime.LoadDINT(a).CompareTo(runtime.LoadDINT(b));
    }
}
