namespace Runtime.IR.RuntimeTypes
{
    public sealed class RuntimeTypeREAL : IComparableRuntimeType
    {
        public static readonly RuntimeTypeREAL Instance = new();
        public string Name => "REAL";
        public int Size => 4;
        public string ReadValue(MemoryLocation location, RTE runtime) => runtime.LoadREAL(location).ToString();
        public override string ToString() => Name;

        public int Compare(MemoryLocation a, MemoryLocation b, RTE runtime) => runtime.LoadREAL(a).CompareTo(runtime.LoadREAL(b));
    }
}
