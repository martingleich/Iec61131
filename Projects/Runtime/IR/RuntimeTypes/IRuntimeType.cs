namespace Runtime.IR.RuntimeTypes
{
    public interface IRuntimeType
    {
        string Name { get; }
        string ReadValue(MemoryLocation location, RTE runtime);
        public IIndexedChildren? GetIndexedChildren() => null;
        int Size { get; }
    }

    public interface IEquatableRuntimeType : IRuntimeType
    {
        bool Equals(MemoryLocation a, MemoryLocation b, RTE runtime);
    }
    public interface IComparableRuntimeType : IRuntimeType
    {
        int Compare(MemoryLocation a, MemoryLocation b, RTE runtime);
    }

    public interface IIndexedChildren
    {
        Range<int> Range { get; }
        MemoryLocation GetChildLocation(MemoryLocation parentLocation, int index);
        IRuntimeType GetChildType(int index);
        string GetChildName(int index);
    }
}
