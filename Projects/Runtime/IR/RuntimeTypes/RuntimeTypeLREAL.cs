namespace Runtime.IR.RuntimeTypes
{
    public sealed class RuntimeTypeLREAL : IComparableRuntimeType
    {
        public static readonly RuntimeTypeLREAL Instance = new();
        public string Name => "LREAL";
        public string ReadValue(MemoryLocation location, Runtime runtime) => runtime.LoadLREAL(location).ToString();
        public override string ToString() => Name;

        public int Compare(MemoryLocation a, MemoryLocation b, Runtime runtime) => runtime.LoadLREAL(a).CompareTo(runtime.LoadLREAL(b));
    }
}
