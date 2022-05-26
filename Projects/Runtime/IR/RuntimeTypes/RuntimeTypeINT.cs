namespace Runtime.IR.RuntimeTypes
{
    public sealed class RuntimeTypeINT : IComparableRuntimeType
    {
        public static readonly RuntimeTypeINT Instance = new();
        public string Name => "INT";
        public int Size => 2;
        public string ReadValue(MemoryLocation location, RTE runtime) => runtime.LoadINT(location).ToString();
        public override string ToString() => Name;

        public int Compare(MemoryLocation a, MemoryLocation b, RTE runtime) => runtime.LoadINT(a).CompareTo(runtime.LoadINT(b));
    }
}
