namespace Compiler.Messages
{
	public sealed class TypeExpectedMessage : ACriticalMessage
	{
		public readonly IToken ReceivedToken;
		public TypeExpectedMessage(IToken receivedToken) : base(receivedToken.SourcePosition)
		{
			ReceivedToken = receivedToken;
		}

		public override string Text => $"Expected a type but found '{ReceivedToken.Generating}' instead.";
	}
}