namespace Runtime.IR.RuntimeTypes
{
    public sealed class RuntimeTypeLINT : IComparableRuntimeType
    {
        public static readonly RuntimeTypeLINT Instance = new();
        public string Name => "LINT";
        public int Size => 8;
        public string ReadValue(MemoryLocation location, RTE runtime) => runtime.LoadLINT(location).ToString();
        public override string ToString() => Name;

        public int Compare(MemoryLocation a, MemoryLocation b, RTE runtime) => runtime.LoadLINT(a).CompareTo(runtime.LoadLINT(b));
    }
}
