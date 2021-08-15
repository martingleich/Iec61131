using System.Diagnostics.CodeAnalysis;

namespace Compiler.Messages
{
	public interface IMessage
	{
		SourcePosition Position { get; }
		string Text { get; }
		bool Critical { get; }
	}

	public abstract class ACriticalMessage : IMessage
	{
		protected ACriticalMessage(SourcePosition position)
		{
			Position = position;
		}

		public SourcePosition Position { get; }
		public abstract string Text { get; }
		public bool Critical => true;
		[ExcludeFromCodeCoverage]
		public override string ToString() => $"{Position} {Text}";
	}

	public abstract class AUncriticalMessage : IMessage
	{
		protected AUncriticalMessage(SourcePosition position)
		{
			Position = position;
		}

		public SourcePosition Position { get; }
		public abstract string Text { get; }
		public bool Critical => false;
		[ExcludeFromCodeCoverage]
		public override string ToString() => $"{Position} {Text}";
	}


	public class InvalidBooleanLiteralMessage : ACriticalMessage
	{
		public InvalidBooleanLiteralMessage(SourcePosition position) : base(position)
		{
		}

		public override string Text => "Expected '0','1','TRUE' or 'FALSE'";
	}
	public class MissingEndOfMultilineCommentMessage : ACriticalMessage
	{
		public readonly string Expected;
		public MissingEndOfMultilineCommentMessage( SourcePosition position, string expected) : base(position)
		{
			Expected = expected;
		}

		public override string Text => $"Could not find the string '{Expected}' terminating the multiline comment.";
	}
	public class MissingEndOfAttributeMessage : ACriticalMessage
	{
		public MissingEndOfAttributeMessage(SourcePosition position) : base(position)
		{
		}

		public override string Text => "Could not find the string '}' terminating the attribute.";
	}
}
