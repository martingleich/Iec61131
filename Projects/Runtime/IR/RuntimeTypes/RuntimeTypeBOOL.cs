namespace Runtime.IR.RuntimeTypes
{
    public sealed class RuntimeTypeBOOL : IEquatableRuntimeType
    {
        public static readonly RuntimeTypeBOOL Instance = new();
        public string Name => "BOOL";
        public string ReadValue(MemoryLocation location, RTE runtime) => runtime.LoadBOOL(location) ? "TRUE" : "FALSE";
        public override string ToString() => Name;

        public bool Equals(MemoryLocation a, MemoryLocation b, RTE runtime) => runtime.LoadBOOL(a).Equals(runtime.LoadBOOL(b));
    }
}
