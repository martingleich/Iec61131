using Compiler;
using Compiler.Messages;

namespace OfflineCompiler
{
    public sealed class SourceMapMessageFormatter : IMessageFormatter
    {
        private readonly SourceMap _sourceMap;

        public SourceMapMessageFormatter(SourceMap sourceMap)
        {
            _sourceMap = sourceMap;
        }

        public string GetKindName(bool critical) => MessageFormatter.Null.GetKindName(critical);
        public string GetSourceName(SourceSpan span) => _sourceMap.GetNameOf(span);
    }
}
