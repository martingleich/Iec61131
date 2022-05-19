namespace Compiler.Messages
{
    public interface IMessageFormatter
	{
		string GetSourceName(SourceSpan span);
		string GetKindName(bool critical);
	}

	public static class MessageFormatter
    {
        public sealed class NullFormatter : IMessageFormatter
        {
            public string GetKindName(bool critical) => critical ? "Error" : "Warning";
            public string GetSourceName(SourceSpan span) => span.ToString();
        }
        public static readonly IMessageFormatter Null = new NullFormatter();
    }
}
