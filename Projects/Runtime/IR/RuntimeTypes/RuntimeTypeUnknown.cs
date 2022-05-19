using System;

namespace Runtime.IR.RuntimeTypes
{
    public sealed class RuntimeTypeUnknown : IRuntimeType
    {
        public RuntimeTypeUnknown(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public string Name { get; }
        public string ReadValue(MemoryLocation location, RTE runtime) => Name;
        public override string ToString() => Name;
    }
}
