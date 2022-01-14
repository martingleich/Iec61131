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
		private readonly object _cacheLock = new();

		public SingleFileCompilerScheduler(string fileName)
		{
			FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
		}

		private TopLevelPouLanguageSource GetPouSource(TextSnapshot snapshot)
			=> new (FileName, snapshot.GetText());

		public ParsedTopLevelInterfaceAndBodyPouLanguageSource GetParsedPou(TextSnapshot snapshot)
		{
			lock (_cacheLock)
			{
				if (LastParsed.HasValue && ReferenceEquals(snapshot, LastParsed.Value.Item2))
					return LastParsed.Value.Item1;
			}
			var source = GetPouSource(snapshot);
			var result = (ParsedTopLevelInterfaceAndBodyPouLanguageSource.FromSource(source), snapshot);
			lock (_cacheLock)
				return (LastParsed = result).Value.Item1;
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
