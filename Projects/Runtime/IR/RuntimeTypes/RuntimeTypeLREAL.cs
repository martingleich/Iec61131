namespace Runtime.IR.RuntimeTypes
{
    public sealed class RuntimeTypeLREAL : IComparableRuntimeType
    {
        public static readonly RuntimeTypeLREAL Instance = new();
        public string Name => "LREAL";
        public int Size => 8;
        public string ReadValue(MemoryLocation location, RTE runtime) => runtime.LoadLREAL(location).ToString();
        public override string ToString() => Name;

        public int Compare(MemoryLocation a, MemoryLocation b, RTE runtime) => runtime.LoadLREAL(a).CompareTo(runtime.LoadLREAL(b));
    }
}
