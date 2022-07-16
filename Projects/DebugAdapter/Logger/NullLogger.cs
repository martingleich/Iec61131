using Microsoft.Extensions.Logging;
using System;

namespace DebugAdapter.Logger
{
    public sealed class NullLogger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) => NullDisposable.Instance;

        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }
}
