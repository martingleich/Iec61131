using Compiler.CodegenIR;
using Compiler.Messages;
using Runtime.IR;
using StandardLibraryExtensions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
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
		public readonly SourceMap? SourceMap;

		private Project(
			CaseInsensitiveString name,
			ImmutableArray<ParsedTopLevelInterfaceAndBodyPouLanguageSource> pous,
			ImmutableArray<ParsedGVLLanguageSource> gvls,
			ImmutableArray<ParsedDutLanguageSource> duts,
			ImmutableArray<LibraryLanguageSource> libraries,
			SourceMap? sourceMap)
		{
			Pous = pous;
			Gvls = gvls;
			Duts = duts;
			Libraries = libraries;
			Name = name;
			SourceMap = sourceMap;
		}

		private ImmutableArray<IMessage>? _backingParseMessages;
		public IEnumerable<IMessage> ParseMessages
		{
			get
			{
				if (!_backingParseMessages.HasValue)
				{
					_backingParseMessages = Enumerable.Concat(Enumerable.Concat(
						Duts.SelectMany(d => d.Messages),
						Pous.SelectMany(d => d.Messages)),
						Gvls.SelectMany(d => d.Messages)).ToImmutableArray();
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
		/// <summary>
		/// Gets the bind messages without forcing full typecheck.
		/// </summary>
		public IEnumerable<IMessage> BindMessages
		{
			get
			{
				foreach (var x in BoundModule.BindMessages)
					yield return x;
			}
		}

		public IMessageFormatter GetMessageFormatter() => SourceMap?.GetMessageFormatter() ?? MessageFormatter.Null;
		public IEnumerable<IMessage> AllMessages => Enumerable.Concat(ParseMessages, BindMessages);
		public bool HasCriticalError() => AllMessages.Any(msg => msg.Critical);
        public CompiledModule GenerateCode()
        {
			if (HasCriticalError())
				throw new InvalidOperationException($"Cannot generate code with critical errors.");
            var runtimeTypeFactory = new RuntimeTypeFactoryFromType();

            var globalAllocationTable = GlobalVariableAllocationTable.Generate(2, BoundModule.Interface, runtimeTypeFactory);
            var globals = globalAllocationTable.ToCompiledGvls().ToImmutableArray();
            var pous = BoundModule.FunctionPous.Values.Concat(BoundModule.FunctionBlockPous.Values)
                .Select(bound => CodegenIR.CodegenIR.GenerateCode(runtimeTypeFactory, globalAllocationTable, SourceMap, bound))
                .ToImmutableArray();

            return new(globals, pous, runtimeTypeFactory.GetTypes());
        }

		public static Project Empty(CaseInsensitiveString name) => new(
			name,
			ImmutableArray<ParsedTopLevelInterfaceAndBodyPouLanguageSource>.Empty,
			ImmutableArray<ParsedGVLLanguageSource>.Empty,
			ImmutableArray<ParsedDutLanguageSource>.Empty,
			ImmutableArray<LibraryLanguageSource>.Empty,
			null);

		public static Project New(CaseInsensitiveString name, IEnumerable<ILanguageSource> sources)
		{
			var forMany = new ProjectAdderMany();
			foreach (var source in sources)
				source.Accept(forMany);
			return forMany.AddTo(name);
		}

        private delegate ILanguageSource LanguageSourceCreator(string sourceName, string name, string content);
        public static Project NewFromFolder(DirectoryInfo folder)
        {
            var sources = new List<ILanguageSource>();
            var maps = new List<SourceMap.SingleFile>();
            foreach (var (lmSource, sourcemap) in folder.EnumerateFiles().Select(ToLanguageSource).WhereNotNullStruct())
            {
                sources.Add(lmSource);
                maps.Add(sourcemap);
            }
            var sourceMap = new SourceMap(maps);

            return New(folder.Name.ToCaseInsensitive(), sources).SetSourceMap(sourceMap);

            static (ILanguageSource, SourceMap.SingleFile)? ToLanguageSource(FileInfo info)
            {
                if (Extension(info.Name, out string remainder).Equals(".ST", StringComparison.InvariantCultureIgnoreCase))
                {
                    LanguageSourceCreator? creator = Extension(remainder, out var name).ToUpperInvariant() switch
                    {
                        ".POU" => ToLanguageSourcePou,
                        ".GVL" => ToLanguageSourceGvl,
                        ".DUT" => ToLanguageSourceDut,
                        _ => null,
                    };

                    if (creator != null)
                    {
                        var content = File.ReadAllText(info.FullName, System.Text.Encoding.UTF8);
                        var sourceMap = SourceMap.SingleFile.Create(info, content);
                        return (creator(info.Name, name, content), sourceMap);
                    }
                }
                return null;

                static TopLevelPouLanguageSource ToLanguageSourcePou(string sourceFile, string name, string content)
                    => new(sourceFile, content);
                static GlobalVariableListLanguageSource ToLanguageSourceGvl(string sourceFile, string name, string content)
                    => new(sourceFile, name.ToCaseInsensitive(), content);
                static DutLanguageSource ToLanguageSourceDut(string sourceFile, string name, string content)
                    => new(sourceFile, content);
                static string Extension(string name, out string remainder)
                {
                    int id = name.LastIndexOf(".");
                    if (id < 0)
                    {
                        remainder = name;
                        return "";
                    }
                    else
                    {
                        remainder = name.Remove(id);
                        return name[id..];
                    }
                }
            }
        }

		public Project SetSourceMap(SourceMap sourceMap)
			=> new(Name, Pous, Gvls, Duts, Libraries, sourceMap);
		public Project Add(ParsedDutLanguageSource source)
			=> new(Name, Pous, Gvls, Duts.Add(source), Libraries, SourceMap);
		public Project Add(ParsedTopLevelInterfaceAndBodyPouLanguageSource source)
			=> new(Name, Pous.Add(source), Gvls, Duts, Libraries, SourceMap);
		public Project Add(ParsedGVLLanguageSource source)
			=> new(Name, Pous, Gvls.Add(source), Duts, Libraries, SourceMap);
		public Project Add(LibraryLanguageSource source)
			=> new(Name, Pous, Gvls, Duts, Libraries.Add(source), SourceMap);
		public Project Add(DutLanguageSource source)
			=> Add(ParsedDutLanguageSource.FromSource(source));
		public Project Add(TopLevelInterfaceAndBodyPouLanguageSource source)
			=> Add(ParsedTopLevelInterfaceAndBodyPouLanguageSource.FromSource(source));
		public Project Add(TopLevelPouLanguageSource source)
			=> Add(ParsedTopLevelInterfaceAndBodyPouLanguageSource.FromSource(source));
		public Project Add(GlobalVariableListLanguageSource source)
			=> Add(ParsedGVLLanguageSource.FromSource(source));

		public Project Add(ILanguageSource source) => source.Accept(ProjectAdder.Instance, this);
		public Project Add(IEnumerable<ILanguageSource> sources)
		{
			var forMany = new ProjectAdderMany();
			foreach (var source in sources)
				source.Accept(forMany);
			return forMany.AddTo(this);
		}

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
		private sealed class ProjectAdderMany : ILanguageSource.IVisitor
		{
			public ProjectAdderMany()
			{
				Pous = ImmutableArray.CreateBuilder<ParsedTopLevelInterfaceAndBodyPouLanguageSource>();
				Gvls = ImmutableArray.CreateBuilder<ParsedGVLLanguageSource>();
				Duts = ImmutableArray.CreateBuilder<ParsedDutLanguageSource>();
			}

			private readonly ImmutableArray<ParsedTopLevelInterfaceAndBodyPouLanguageSource>.Builder Pous;
			private readonly ImmutableArray<ParsedGVLLanguageSource>.Builder Gvls;
			private readonly ImmutableArray<ParsedDutLanguageSource>.Builder Duts;

			public Project AddTo(Project p) => new (
					p.Name,
					p.Pous.AddRange(Pous),
					p.Gvls.AddRange(Gvls),
					p.Duts.AddRange(Duts),
					p.Libraries,
					p.SourceMap);
			public Project AddTo(CaseInsensitiveString name) => new (
					name,
					Pous.ToImmutable(),
					Gvls.ToImmutable(),
					Duts.ToImmutable(),
					ImmutableArray<LibraryLanguageSource>.Empty,
					null);

			public void Visit(TopLevelInterfaceAndBodyPouLanguageSource topLevelInterfaceAndBodyPouLanguageSource)
				=> Pous.Add(ParsedTopLevelInterfaceAndBodyPouLanguageSource.FromSource(topLevelInterfaceAndBodyPouLanguageSource));
			public void Visit(GlobalVariableListLanguageSource globalVariableLanguageSource)
				=> Gvls.Add(ParsedGVLLanguageSource.FromSource(globalVariableLanguageSource));
			public void Visit(DutLanguageSource dutLanguageSource)
				=> Duts.Add(ParsedDutLanguageSource.FromSource(dutLanguageSource));
			public void Visit(TopLevelPouLanguageSource topLevelPouLanguageSource)
				=> Pous.Add(ParsedTopLevelInterfaceAndBodyPouLanguageSource.FromSource(topLevelPouLanguageSource));
		}
	}
}
