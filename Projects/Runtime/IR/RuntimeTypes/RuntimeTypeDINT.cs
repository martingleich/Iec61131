﻿namespace Runtime.IR.RuntimeTypes
{
    public sealed class RuntimeTypeDINT : IComparableRuntimeType
    {
        public static readonly RuntimeTypeDINT Instance = new();
        public string Name => "DINT";
        public int Size => 4;
        public string ReadValue(MemoryLocation location, RTE runtime) => runtime.LoadDINT(location).ToString();
        public override string ToString() => Name;

        public int Compare(MemoryLocation a, MemoryLocation b, RTE runtime) => runtime.LoadDINT(a).CompareTo(runtime.LoadDINT(b));
    }
}
