using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Compiler
{
	public sealed class Project
	{
		private readonly ImmutableArray<TopLevelInterfaceAndBodyPouLanguageSource> Pous;
		private readonly ImmutableArray<GlobalVariableLanguageSource> Gvls;
		private readonly ImmutableArray<DutLanguageSource> Duts;

		public readonly Lazy<LazyBoundModule> LazyBoundModule;

		private Project(ImmutableArray<TopLevelInterfaceAndBodyPouLanguageSource> pous, ImmutableArray<GlobalVariableLanguageSource> gvls, ImmutableArray<DutLanguageSource> duts)
		{
			Pous = pous;
			Gvls = gvls;
			Duts = duts;

			LazyBoundModule = new Lazy<LazyBoundModule>(() => ProjectBinder.Bind(Pous, Gvls, Duts));
		}

		public static readonly Project Empty = new (
			ImmutableArray<TopLevelInterfaceAndBodyPouLanguageSource>.Empty,
			ImmutableArray<GlobalVariableLanguageSource>.Empty,
			ImmutableArray<DutLanguageSource>.Empty);

		public Project Add(DutLanguageSource source) => new (Pous, Gvls, Duts.Add(source));
		
		public Project Add(params ILanguageSource[] sources) => Add(sources.AsEnumerable());
		public Project Add(IEnumerable<ILanguageSource> sources) => ProjectBuilder.Build(this, sources);

		private sealed class ProjectBuilder : ILanguageSource.IVisitor
		{
			private readonly ImmutableArray<TopLevelInterfaceAndBodyPouLanguageSource>.Builder Pous = ImmutableArray.CreateBuilder<TopLevelInterfaceAndBodyPouLanguageSource>();
			private readonly ImmutableArray<GlobalVariableLanguageSource>.Builder Gvls = ImmutableArray.CreateBuilder<GlobalVariableLanguageSource>();
			private readonly ImmutableArray<DutLanguageSource>.Builder Duts = ImmutableArray.CreateBuilder<DutLanguageSource>();

			public void Visit(TopLevelInterfaceAndBodyPouLanguageSource topLevelInterfaceAndBodyPouLanguageSource)
			{
				Pous.Add(topLevelInterfaceAndBodyPouLanguageSource);
			}

			public void Visit(GlobalVariableLanguageSource globalVariableLanguageSource)
			{
				Gvls.Add(globalVariableLanguageSource);
			}

			public void Visit(DutLanguageSource dutLanguageSource)
			{
				Duts.Add(dutLanguageSource);
			}

			public static Project Build(Project project, IEnumerable<ILanguageSource> sources)
			{
				var builder = new ProjectBuilder();
				foreach (var source in sources)
					source.Accept(builder);
				return new(
					project.Pous.AddRange(builder.Pous),
					project.Gvls.AddRange(builder.Gvls),
					project.Duts.AddRange(builder.Duts));
			}
		}
	}
}
