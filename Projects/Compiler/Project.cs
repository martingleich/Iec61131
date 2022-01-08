using Compiler.Messages;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Compiler
{
	public sealed class Project
	{
		private readonly ImmutableArray<ParsedTopLevelInterfaceAndBodyPouLanguageSource> Pous;
		private readonly ImmutableArray<ParsedGVLLanguageSource> Gvls;
		private readonly ImmutableArray<ParsedDutLanguageSource> Duts;
		private readonly ImmutableArray<LibraryLanguageSource> Libraries;
		public readonly CaseInsensitiveString Name;

		public readonly Lazy<BoundModule> LazyBoundModule;
		public readonly Lazy<ImmutableArray<IMessage>> LazyParseMessages;

		private Project(
			CaseInsensitiveString name,
			ImmutableArray<ParsedTopLevelInterfaceAndBodyPouLanguageSource> pous,
			ImmutableArray<ParsedGVLLanguageSource> gvls,
			ImmutableArray<ParsedDutLanguageSource> duts,
			ImmutableArray<LibraryLanguageSource> libraries)
		{
			Pous = pous;
			Gvls = gvls;
			Duts = duts;
			Libraries = libraries;
			Name = name;

			LazyBoundModule = new Lazy<BoundModule>(() => ProjectBinder.Bind(Name, Pous, Gvls, Duts, Libraries));
			LazyParseMessages = new Lazy<ImmutableArray<IMessage>>(() =>
				Enumerable.Concat(
					Duts.SelectMany(d => d.Messages),
					Pous.SelectMany(d => d.Messages)).ToImmutableArray());
		}

		public static Project Empty(CaseInsensitiveString name) => new(
			name,
			ImmutableArray<ParsedTopLevelInterfaceAndBodyPouLanguageSource>.Empty,
			ImmutableArray<ParsedGVLLanguageSource>.Empty,
			ImmutableArray<ParsedDutLanguageSource>.Empty,
			ImmutableArray<LibraryLanguageSource>.Empty);

		public Project Add(ParsedDutLanguageSource source)
			=> new(Name, Pous, Gvls, Duts.Add(source), Libraries);
		public Project Add(ParsedTopLevelInterfaceAndBodyPouLanguageSource source)
			=> new(Name, Pous.Add(source), Gvls, Duts, Libraries);
		public Project Add(ParsedGVLLanguageSource source)
			=> new(Name, Pous, Gvls.Add(source), Duts, Libraries);
		public Project Add(LibraryLanguageSource source)
			=> new(Name, Pous, Gvls, Duts, Libraries.Add(source));
		public Project Add(DutLanguageSource source)
			=> Add(ParsedDutLanguageSource.FromSource(source));
		public Project Add(TopLevelInterfaceAndBodyPouLanguageSource source)
			=> Add(ParsedTopLevelInterfaceAndBodyPouLanguageSource.FromSource(source));
		public Project Add(TopLevelPouLanguageSource source)
			=> Add(ParsedTopLevelInterfaceAndBodyPouLanguageSource.FromSource(source));
		public Project Add(GlobalVariableListLanguageSource source)
			=> Add(ParsedGVLLanguageSource.FromSource(source));
	}
}
