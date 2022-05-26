using System;

namespace Runtime.IR.RuntimeTypes
{
    public sealed class RuntimeTypeUnknown : IRuntimeType
    {
        public RuntimeTypeUnknown(string name, int size)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Size = size;
        }

        public string Name { get; }
        public int Size { get; }
        public string ReadValue(MemoryLocation location, RTE runtime) => Name;
        public override string ToString() => Name;
    }
}
