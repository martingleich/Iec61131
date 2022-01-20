using Compiler.Messages;
using System;
using System.Collections.Generic;
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
		}

		private ImmutableArray<IMessage>? _backingParseMessages;
		public IEnumerable<IMessage> ParseMessages
		{
			get
			{
				if (!_backingParseMessages.HasValue)
				{
					_backingParseMessages = Enumerable.Concat(
						Duts.SelectMany(d => d.Messages),
						Pous.SelectMany(d => d.Messages)).ToImmutableArray();
				}
				return _backingParseMessages.Value;
			}
		}
		private BoundModule? _backingBoundModule;
		public BoundModule BoundModule
		{
			get
			{
				if (_backingBoundModule == null)
					_backingBoundModule = ProjectBinder.Bind(Name, Pous, Gvls, Duts, Libraries);
				return _backingBoundModule;
			}
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

		public Project Add(ILanguageSource source) => source.Accept(ProjectAdder.Instance, this);

		private sealed class ProjectAdder : ILanguageSource.IVisitor<Project, Project>
		{
			public static readonly ProjectAdder Instance = new();
			public Project Visit(TopLevelInterfaceAndBodyPouLanguageSource topLevelInterfaceAndBodyPouLanguageSource, Project context)
				=> context.Add(topLevelInterfaceAndBodyPouLanguageSource);
			public Project Visit(GlobalVariableListLanguageSource globalVariableLanguageSource, Project context)
				=> context.Add(globalVariableLanguageSource);
			public Project Visit(DutLanguageSource dutLanguageSource, Project context)
				=> context.Add(dutLanguageSource);
			public Project Visit(TopLevelPouLanguageSource topLevelPouLanguageSource, Project context)
				=> context.Add(topLevelPouLanguageSource);
		}
	}
}
