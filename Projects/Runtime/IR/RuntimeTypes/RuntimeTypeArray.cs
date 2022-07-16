using Superpower;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Runtime.IR.RuntimeTypes
{
    public class RuntimeTypeArray : IRuntimeType
    {
        private class IndexedChildren : IIndexedChildren
        {
            private readonly RuntimeTypeArray Owner;

            public IndexedChildren(RuntimeTypeArray owner)
            {
                Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }

            public Range<int> Range => IR.Range.Create(0, Owner.ElementCount);
            public MemoryLocation GetChildLocation(MemoryLocation parentLocation, int index) => parentLocation + index * Owner.BaseType.Size;
            public string GetChildName(int index)
            {
                string result = "";
                foreach (var dim in Owner.Ranges)
                {
                    int indexDim = index % dim.Size + dim.LowerBound;
                    index /= dim.Size;
                    if (result.Length != 0)
                        result += ", ";
                    result += indexDim;
                }
                return "[" + result + "]";
            }

            public IRuntimeType GetChildType(int index) => Owner.BaseType;
        }
        public readonly ImmutableArray<ArrayTypeRange> Ranges;
        public readonly IRuntimeType BaseType;
        public int ElementCount => Ranges.Aggregate(1, (s, d) => s * d.Size);

        public RuntimeTypeArray(ImmutableArray<ArrayTypeRange> ranges, IRuntimeType baseType)
        {
            Ranges = ranges;
            BaseType = baseType ?? throw new ArgumentNullException(nameof(baseType));
        }

        public string Name => $"ARRAY[{string.Join(", ", Ranges)}] OF {BaseType.Name}";

        public string ReadValue(MemoryLocation location, RTE runtime) => "";

        public int Size => BaseType.Size * ElementCount;

        public IIndexedChildren? GetIndexedChildren() => new IndexedChildren(this);
        public override string ToString() => Name;
    }
}
