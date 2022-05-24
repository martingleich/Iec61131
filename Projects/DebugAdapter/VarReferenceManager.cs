using System;
using System.Collections.Generic;

namespace DebugAdapter
{
    public sealed class VarReferenceManager
    {
        private readonly int _frameCount;
        private readonly List<(int Index, int Parent)> _references = new();
        private readonly int _nextId;

        private const int GLOBAL_ID = 1;
        private const int VAR_REFERENCES_PER_FRAME = 2;
        private const int FIRST_FRAME_VAR_REF = 2;
        private const int ARG_OFFSET = 0;
        private const int LOCAL_OFFSET = 1;
        private int FIRST_USER_VARIABLE => FIRST_FRAME_VAR_REF + VAR_REFERENCES_PER_FRAME * _frameCount;

        public VarReferenceManager(int frameCount)
        {
            if (frameCount < 0)
                throw new ArgumentException($"{nameof(frameCount)}({frameCount}) must be non-negative.");
            _frameCount = frameCount;
            _nextId = frameCount * VAR_REFERENCES_PER_FRAME + FIRST_FRAME_VAR_REF;
        }
        public VarReference Globals => new(this, GLOBAL_ID);
        public VarReference ArgumentsFrame(int frameId) => new(this, frameId * VAR_REFERENCES_PER_FRAME + FIRST_FRAME_VAR_REF + ARG_OFFSET); // 
        public VarReference LocalsFrame(int frameId) => new(this, frameId * VAR_REFERENCES_PER_FRAME + FIRST_FRAME_VAR_REF + LOCAL_OFFSET);

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
                    children.Add(new VarReference(this, i + FIRST_USER_VARIABLE));
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

            public bool IsGlobal => Id == GLOBAL_ID;
            public bool IsStack(out int frameId, out bool isArg)
            {
                if (Id >= FIRST_FRAME_VAR_REF && Id < _owner.FIRST_USER_VARIABLE)
                {
                    int realId = Id - FIRST_FRAME_VAR_REF;
                    isArg = realId % VAR_REFERENCES_PER_FRAME == ARG_OFFSET;
                    if (isArg)
                        frameId = (realId - ARG_OFFSET) / VAR_REFERENCES_PER_FRAME;
                    else
                        frameId = (realId - LOCAL_OFFSET) / VAR_REFERENCES_PER_FRAME;
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
                if (Id >= _owner.FIRST_USER_VARIABLE)
                {
                    var (index, parentId) = _owner._references[Id - _owner.FIRST_USER_VARIABLE];
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
