using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Subjects;
using Compiler;
using Compiler.Messages;
using SyntaxEditor;

namespace FullEditor
{
	public sealed class SingleFileCompilerScheduler
	{
		public readonly string FileName;
		private (ParsedTopLevelInterfaceAndBodyPouLanguageSource, TextSnapshot)? LastParsed;

		public SingleFileCompilerScheduler(string fileName)
		{
			FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
		}

		private TopLevelPouLanguageSource GetPouSource(TextSnapshot snapshot)
			=> new (FileName, snapshot.GetText());

		public ParsedTopLevelInterfaceAndBodyPouLanguageSource GetParsedPou(TextSnapshot snapshot)
		{
			if (!(LastParsed.HasValue && ReferenceEquals(snapshot, LastParsed.Value.Item2)))
			{
				var source = GetPouSource(snapshot);
				LastParsed = (ParsedTopLevelInterfaceAndBodyPouLanguageSource.FromSource(source), snapshot);
			}
			return LastParsed.Value.Item1;
		}

		public void SetNewSnapshot(TextSnapshot snapshot)
		{
			OnNewMessages.OnNext(GetAllMessages(snapshot));
		}

		public ImmutableArray<ProjectMessage> GetAllMessages(TextSnapshot snapshot)
		{
			var parsed = GetParsedPou(snapshot);
			var project = Project.Empty("project".ToCaseInsensitive()).Add(parsed);

			var messages = ImmutableArray.CreateBuilder<IMessage>();
			messages.AddRange(project.ParseMessages);
			messages.AddRange(project.BoundModule.InterfaceMessages);
			messages.AddRange(project.BoundModule.BindMessages);
			return messages.Select(msg => new ProjectMessage(msg, snapshot)).ToImmutableArray();
		}

		public readonly Subject<ImmutableArray<ProjectMessage>> OnNewMessages = new();
	}
}
