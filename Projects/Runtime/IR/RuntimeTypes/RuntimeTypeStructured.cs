using System;
using System.Collections.Immutable;

namespace Runtime.IR.RuntimeTypes
{
    public sealed class RuntimeTypeStructured : IRuntimeType
    {
        public readonly record struct Property(string Name, IRuntimeType Type, int Offset) { }
        public sealed class PropertiesT : IIndexedChildren
        {
            public readonly ImmutableArray<Property> Values;

            public PropertiesT(ImmutableArray<Property> values)
            {
                Values = values;
            }

            public Range<int> Range => new (0, Values.Length);
            public MemoryLocation GetChildLocation(MemoryLocation parentLocation, int index) => parentLocation + Values[index].Offset;
            public string GetChildName(int index) => Values[index].Name;
            public IRuntimeType GetChildType(int index) => Values[index].Type;
        }
        public readonly PropertiesT Properties;

        public RuntimeTypeStructured(string name, int size, ImmutableArray<Property> properties)
        {
            Properties = new PropertiesT(properties);
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Size = size;
        }

        public string Name { get; }
        public int Size { get; }
        public string ReadValue(MemoryLocation location, RTE runtime) => Name;
        public IIndexedChildren? GetIndexedChildren() => Properties;
        public override string ToString() => Name;
    }
}
