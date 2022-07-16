using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace DebugAdapter.Logger
{
    public sealed class SimpleFileLogger : ILogger, IDisposable
    {
        private StreamWriter? _stream;
        public SimpleFileLogger(FileInfo path)
        {
            var fileStream = path.Open(FileMode.Create, FileAccess.Write, FileShare.Read);
            _stream = new StreamWriter(fileStream, System.Text.Encoding.UTF8);
            _stream.AutoFlush = true;
        }
        public LogLevel LogLevel { get; set; }
        public IDisposable BeginScope<TState>(TState state) => NullDisposable.Instance;

        public bool IsEnabled(LogLevel logLevel) => logLevel > LogLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (_stream == null)
                throw new ObjectDisposedException(nameof(SimpleFileLogger));
            var time = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
            var text = formatter(state, null);
            if (exception != null)
                text = text + ": " + exception.ToString();
            var prefix = $"{time} {logLevel}: ";
            var spaceprefix = new string(' ', prefix.Length);
            text = string.Join(Environment.NewLine, text.Split(Environment.NewLine).Select((txt, idx) => idx > 0 ? spaceprefix + txt : txt));
            lock (_stream)
            {
                _stream.WriteLine(prefix + text);
            }
        }

        public void Dispose()
        {
            if (_stream != null)
            {
                lock (_stream)
                {
                    _stream.Dispose();
                    _stream = null;
                }
            }
        }
    }
}
