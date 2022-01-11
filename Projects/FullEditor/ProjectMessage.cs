using Compiler.Messages;
using SyntaxEditor;
using System;

namespace FullEditor
{
	public class ProjectMessage
	{
		public readonly IMessage OriginalMessage;
		public readonly TextSnapshot Snapshot;
		private readonly SnapshotSpan? SnapshotSpan;

		public ProjectMessage(IMessage originalMessage, TextSnapshot snapshot)
		{
			OriginalMessage = originalMessage ?? throw new ArgumentNullException(nameof(originalMessage));
			Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
			SnapshotSpan = snapshot != null
				? new SnapshotSpan(snapshot, originalMessage.Span.ToOffsetSpan())
				: null; 
		}

		public IntSpan? TryGetPosition(TextSnapshot futureSnapshot) => SnapshotSpan?.Get(futureSnapshot);
	}
}
