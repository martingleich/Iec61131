using Runtime.IR;
using Runtime.IR.RuntimeTypes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace DebugAdapter
{
    public class VariableReferenceManager
    {
        private readonly Dictionary<int, VariableReference> Table = new();
        private VariableReference.ScopeVariableReference? _globalScope;
        private readonly Dictionary<(int, bool), VariableReference.ScopeVariableReference> FrameTable = new();
        private int _nextId = 1;

        public T Create<T>(Func<Id, T> factory) where T:VariableReference
        {
            var id = new Id(this, _nextId++);
            var value = factory(id);
            Table.Add(id.Value, value);
            return value;
        }

        public readonly struct Id
        {
            public readonly VariableReferenceManager Owner;
            public readonly int Value;

            public Id(VariableReferenceManager owner, int value)
            {
                Owner = owner;
                Value = value;
            }
        }

        public VariableReference.ScopeVariableReference GetGlobal(ImmutableArray<CompiledGlobalVariableList> gvls)
        {
            if (_globalScope == null)
                _globalScope = Create(id => VariableReference.CreateGlobalVariables(id, gvls));
            return _globalScope;
        }
        public VariableReference.ScopeVariableReference GetStack(int frame, bool args, MemoryLocation stackBase, IEnumerable<VariableTable.StackVariable> variables)
        {
            if (!FrameTable.TryGetValue((frame, args), out var frameScope))
            {
                frameScope = Create(id =>
                {
                    var children = variables.Select(v => id.Owner.Create(id2 => VariableReference.CreateForType(id2, v.Name, v.Name, v.Type, stackBase + v.StackOffset))).ToImmutableArray();
                    return new VariableReference.ScopeVariableReference(args ? "Arguments" : "Locals", id, children);
                });
                FrameTable.Add((frame, args), frameScope);
            }
            return frameScope;
        }
        public VariableReference Get(int id) => Table[id];
    }

    public abstract class VariableReference
    {
        public readonly VariableReferenceManager.Id Id;

        protected VariableReference(VariableReferenceManager.Id id)
        {
            Id = id;
        }

        public abstract IEnumerable<VariableReference> GetChildren();
        public abstract int ChildCount { get; }
        public abstract (MemoryLocation, IRuntimeType)? ValueRequest { get; }
        public abstract string Name { get; }
        public abstract string Path { get; }
        public abstract IRuntimeType? Type { get; }

        public class ScopeVariableReference : VariableReference
        {
            public ScopeVariableReference(string name, VariableReferenceManager.Id id, ImmutableArray<VariableReference> children) : base(id)
            {
                Children = children;
                Name = name;
            }

            public ImmutableArray<VariableReference> Children { get; }
            public override IEnumerable<VariableReference> GetChildren() => Children;
            public override int ChildCount => Children.Length;
            public override (MemoryLocation, IRuntimeType)? ValueRequest => null;
            public override string Name { get; }
            public override string Path => "";
            public override IRuntimeType? Type => null;

            public Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages.Scope GetScope() =>
                new (Name, Id.Value, false)
                {
                    NamedVariables = ChildCount
                };
        }
        public class GVLVariableReference : VariableReference
        {
            public GVLVariableReference(VariableReferenceManager.Id id, string path, CompiledGlobalVariableList variableList, MemoryLocation location) : base(id)
            {
                Path = path;
                VariableList = variableList;
                Location = location;
                ChildReferences = new(() =>
                    VariableList.VariableTable!.Value
                    .Select(v => Id.Owner.Create(id => CreateForType(id, Path + "::" + v.Name, v.Name, v.Type, Location + v.Offset)))
                    .ToImmutableArray());
            }

            public CompiledGlobalVariableList VariableList { get; }
            public override string Path { get; }
            public override string Name => VariableList.Name;
            public override IRuntimeType? Type => null;
            public MemoryLocation Location { get; }
            private readonly Lazy<ImmutableArray<VariableReference>> ChildReferences;
            public override IEnumerable<VariableReference> GetChildren() => ChildReferences.Value;
            public override int ChildCount => VariableList.VariableTable?.Length ?? 0;
            public override (MemoryLocation, IRuntimeType)? ValueRequest => null;
        }
        public class IndexedVariableReference : VariableReference
        {
            public IndexedVariableReference(VariableReferenceManager.Id id, string path, string name, IRuntimeType type, IIndexedChildren children, MemoryLocation location) : base(id)
            {
                Path = path ?? throw new ArgumentNullException(nameof(path));
                Name = name ?? throw new ArgumentNullException(nameof(name));
                Type = type ?? throw new ArgumentNullException(nameof(type));
                Children = children ?? throw new ArgumentNullException(nameof(children));
                Location = location;
                ChildReferences = new(() =>
                    Children.Range.ToEnumerable()
                    .Select(i => Id.Owner.Create(id =>
                    {
                        var childName = Children.GetChildName(i);
                        var childType = Children.GetChildType(i);
                        var childLocation = Children.GetChildLocation(Location, i);
                        return CreateForType(id, Path + childName, childName, childType, childLocation);
                    }))
                    .ToImmutableArray());
            }

            public override string Path { get; }
            public override string Name { get; }
            public override IRuntimeType Type { get; }
            public IIndexedChildren Children { get; }
            public MemoryLocation Location { get; }
            private readonly Lazy<ImmutableArray<VariableReference>> ChildReferences;
            public override IEnumerable<VariableReference> GetChildren() => ChildReferences.Value;
            public override int ChildCount => Children.Range.GetLength();
            public override (MemoryLocation, IRuntimeType)? ValueRequest => (Location, Type);
        }
        public class SimpleVariableReference : VariableReference
        {
            public SimpleVariableReference(VariableReferenceManager.Id id, string path, string name, IRuntimeType type, MemoryLocation location) : base(id)
            {
                Path = path;
                Name = name;
                Type = type;
                Location = location;
            }

            public override string Path { get; }
            public override string Name { get; }
            public override IRuntimeType Type { get; }
            public MemoryLocation Location { get; }
            public override IEnumerable<VariableReference> GetChildren() => Enumerable.Empty<VariableReference>();
            public override int ChildCount => 0;
            public override (MemoryLocation, IRuntimeType)? ValueRequest => (Location, Type);
        }
    
        public static VariableReference CreateForType(VariableReferenceManager.Id id, string path, string name, IRuntimeType type, MemoryLocation location)
        {
            if (type.GetIndexedChildren() is IIndexedChildren subChildren)
            {
                return new IndexedVariableReference(
                    id,
                    path,
                    name,
                    type,
                    subChildren,
                    location);
            }
            else
            {
                return new SimpleVariableReference(
                    id,
                    path,
                    name,
                    type,
                    location);
            }
        }
        public static ScopeVariableReference CreateGlobalVariables(VariableReferenceManager.Id id, ImmutableArray<CompiledGlobalVariableList> gvls)
        {
            var children = gvls.Select(v => id.Owner.Create<VariableReference>(id2 => new GVLVariableReference(id2, v.Name, v, new MemoryLocation(v.Area, 0)))).ToImmutableArray();
            return new ScopeVariableReference("Globals", id, children);
        }
    }
}
