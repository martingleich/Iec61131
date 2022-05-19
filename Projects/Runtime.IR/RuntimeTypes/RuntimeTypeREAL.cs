namespace Runtime.IR.RuntimeTypes
{
    public sealed class RuntimeTypeREAL : IComparableRuntimeType
    {
        public static readonly RuntimeTypeREAL Instance = new();
        public string Name => "REAL";
        public string ReadValue(MemoryLocation location, Runtime runtime) => runtime.LoadREAL(location).ToString();
        public override string ToString() => Name;

        public int Compare(MemoryLocation a, MemoryLocation b, Runtime runtime) => runtime.LoadREAL(a).CompareTo(runtime.LoadREAL(b));
    }
}
