using System;

namespace DebugAdapter.Logger
{
    public sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();
        public void Dispose() { }
    }
}
