using System;
using System.Collections.Generic;

namespace Runtime
{
    public sealed class VarReferenceManager
    {
        private readonly int _frameCount;
        private readonly List<(int Index, int Parent)> _references = new();
        private readonly int _nextId;

        public VarReferenceManager(int frameCount)
        {
            if (frameCount < 0)
                throw new ArgumentException($"{nameof(frameCount)}({frameCount}) must be non-negative.");
            _frameCount = frameCount;
            _nextId = frameCount * 2 + 1;
        }
        public VarReference Globals => new(this, 0);
        public VarReference ArgumentsFrame(int frameId) => new(this, frameId * 2 + 1);
        public VarReference LocalsFrame(int frameId) => new(this, frameId * 2 + 2);

        private VarReference? GetChild(VarReference owner, int id)
        {
            for (int i = 0; i < _references.Count; ++i)
                if (_references[i].Parent == owner.Id && _references[i].Index == id)
                    return new(this, i);
            return null;
        }
        public IEnumerable<VarReference> AllocateChildren(VarReference owner, int start, int count)
        {
            var children = new List<VarReference>();
            for (int i = start; i < start + count; ++i)
            {
                if (GetChild(owner, i) is VarReference childRef)
                {
                    children.Add(childRef);
                }
                else
                {
                    _references.Add((i, owner.Id));
                    children.Add(new VarReference(this, i));
                }
            }
            return children;
        }

        public VarReference Get(int id) => new(this, id);
        public readonly struct VarReference
        {
            private readonly VarReferenceManager _owner;
            public readonly int Id;

            public VarReference(VarReferenceManager owner, int id)
            {
                if (id < 0)
                    throw new ArgumentException($"{nameof(id)}({id}) must be non-negative.");
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
                Id = id;
            }

            public bool IsGlobal => Id == 0;
            public bool IsStack(out int frameId, out bool isArg)
            {
                if (Id > 0 && Id <= _owner._frameCount * 2)
                {
                    isArg = Id % 2 != 0;
                    if (isArg)
                        frameId = (Id - 1) / 2;
                    else
                        frameId = (Id - 2) / 2;
                    return true;
                }
                else
                {
                    frameId = default;
                    isArg = default;
                    return false;
                }
            }
            public bool IsLocal(out int frameId) => IsStack(out frameId, out bool isArg) && !isArg;
            public bool IsArgument(out int frameId) => IsStack(out frameId, out bool isArg) && isArg;
            public bool IsChild(out VarReference parent, out int childId)
            {
                if (Id >= _owner._nextId)
                {
                    var (index, parentId) = _owner._references[Id - _owner._nextId];
                    childId = index;
                    parent = new VarReference(_owner, parentId);
                    return true;
                }
                else
                {
                    parent = default;
                    childId = 0;
                    return false;
                }
            }
        }
    }
}
