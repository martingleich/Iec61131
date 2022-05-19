using System;
using System.IO;
using Compiler;
using System.Linq;
using StandardLibraryExtensions;
using System.Collections.Generic;
using System.Collections.Immutable;
using Runtime.IR;

namespace OfflineCompiler
{
    public class OfflineCompilerProject
    {
        private readonly Project _compilerProject;
        private readonly SourceMap _sourceMap;

        public OfflineCompilerProject(Project compilerProject, SourceMap sourceMap)
        {
            _compilerProject = compilerProject ?? throw new ArgumentNullException(nameof(compilerProject));
            _sourceMap = sourceMap ?? throw new ArgumentNullException(nameof(sourceMap));
        }

        private delegate ILanguageSource LanguageSourceCreator(string sourceName, string name, string content);
        public static OfflineCompilerProject FromFolder(DirectoryInfo folder)
        {
            var sourceMap = new SourceMap();
            var sources = new List<ILanguageSource>();
            foreach (var (lmSource, sourcemap) in folder.EnumerateFiles().Select(ToLanguageSource).WhereNotNullStruct())
            {
                sources.Add(lmSource);
                sourceMap.Add(sourcemap);
            }

            return new(Project.New(folder.Name.ToCaseInsensitive(), sources), sourceMap);

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

        public ImmutableArray<string> GetAllFormattedMessages()
        {
            var formatter = new SourceMapMessageFormatter(_sourceMap);
            return _compilerProject.AllMessages.Select(m => m.ToString(formatter)).ToImmutableArray();
        }
        public bool Check(TextWriter stdout)
        {
            var formatter = new SourceMapMessageFormatter(_sourceMap);
            bool isOkay = true;
            foreach (var msg in _compilerProject.AllMessages)
            {
                stdout.WriteLine(msg.ToString(formatter));
                isOkay &= !msg.Critical;
            }
            return isOkay;
        }

        public CompiledModule GenerateCode()
        {
            var runtimeTypeFactory = new RuntimeTypeFactory(_compilerProject.BoundModule.Interface.SystemScope);

            var globalAllocationTable = GlobalVariableAllocationTable.Generate(2, _compilerProject.BoundModule.Interface, runtimeTypeFactory);
            var globals = globalAllocationTable.ToCompiledGvls().ToImmutableArray();
            var pous = _compilerProject.BoundModule.FunctionPous.Values
                .Select(bound => CodegenIR.GenerateCode(runtimeTypeFactory, globalAllocationTable, _sourceMap, bound))
                .ToImmutableArray();

            return new(globals, pous);
        }
    }
}
