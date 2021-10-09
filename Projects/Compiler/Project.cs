using Compiler.Messages;
using Compiler.Scopes;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Compiler
{
	public sealed class Project
	{
		private readonly ImmutableArray<ParsedTopLevelInterfaceAndBodyPouLanguageSource> Pous;
		private readonly ImmutableArray<GlobalVariableLanguageSource> Gvls;
		private readonly ImmutableArray<ParsedDutLanguageSource> Duts;

		public readonly Lazy<BoundModule> LazyBoundModule;
		public readonly Lazy<ImmutableArray<IMessage>> LazyParseMessages;

		private Project( ImmutableArray<ParsedTopLevelInterfaceAndBodyPouLanguageSource> pous, ImmutableArray<GlobalVariableLanguageSource> gvls, ImmutableArray<ParsedDutLanguageSource> duts)
		{
			Pous = pous;
			Gvls = gvls;
			Duts = duts;

			LazyBoundModule = new Lazy<BoundModule>(() => ProjectBinder.Bind(Pous, Gvls, Duts));
			LazyParseMessages = new Lazy<ImmutableArray<IMessage>>(() =>
				Enumerable.Concat(
					Duts.SelectMany(d => d.Messages),
					Pous.SelectMany(d => d.Messages)).ToImmutableArray());
		}

		public static readonly Project Empty = new (
			ImmutableArray<ParsedTopLevelInterfaceAndBodyPouLanguageSource>.Empty,
			ImmutableArray<GlobalVariableLanguageSource>.Empty,
			ImmutableArray<ParsedDutLanguageSource>.Empty);

		public Project Add(ParsedDutLanguageSource source) => new (Pous, Gvls, Duts.Add(source));
		public Project Add(ParsedTopLevelInterfaceAndBodyPouLanguageSource source) => new (Pous.Add(source), Gvls, Duts);
		public Project Add(DutLanguageSource source)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));
			var msg = new MessageBag();
			var parsed = Parser.ParseTypeDeclaration(source.Source, msg);
			return Add(new ParsedDutLanguageSource(source, parsed, msg.ToImmutable()));
		}
		public Project Add(TopLevelInterfaceAndBodyPouLanguageSource source)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));
			var msg = new MessageBag();
			var itf = Parser.ParsePouInterface(source.Interface, msg);
			var body = Parser.ParsePouBody(source.Body, msg);
			return Add(new ParsedTopLevelInterfaceAndBodyPouLanguageSource(source, itf, body, msg.ToImmutable()));
		}
	}
}
