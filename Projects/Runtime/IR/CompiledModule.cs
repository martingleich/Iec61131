using System.IO;
using StandardLibraryExtensions;
using System.Collections.Immutable;
using Runtime.IR.RuntimeTypes;
using System.Collections.Generic;
using Runtime.IR.Xml;
using System.Linq;

namespace Runtime.IR
{
    public sealed record CompiledModule(
        ImmutableArray<CompiledGlobalVariableList> GlobalVariableLists,
        ImmutableArray<CompiledPou> Pous,
        ImmutableArray<RuntimeTypeStructured> Types)
    {
        private const string GvlEnding = "gvl.ir.xml";
        private const string PouEnding = "pou.ir.xml";
        private const string TypeEnding = "type.ir.xml";
        public void WriteToDictionary(DirectoryInfo path)
        {
            path.Create();
            foreach (var gvl in GlobalVariableLists)
            {
                var file = path.FileInfo($"{gvl.Name}.{GvlEnding}");
                using var stream = file.Create();
                Xml.XmlGlobalVariableList.ToXml(gvl, stream);
            }
            foreach (var pou in Pous)
            {
                var file = path.FileInfo($"{pou.Id.Name.Split("::")[^1]}.{PouEnding}");
                using var stream = file.Create();
                Xml.XmlCompiledPou.ToXml(pou, stream);
            }
            foreach (var type in Types)
            {
                var file = path.FileInfo($"{type.Name}.{TypeEnding}");
                using var stream = file.Create();
                Xml.XmlCompiledType.ToXml(type, stream);
            }
        }
        public static CompiledModule LoadFromDirectory(DirectoryInfo folder)
        {
            var types = LoadTypes(folder);
            var typeParser = new RuntimeTypeParser(types.ToDictionary(v => v.Name, v => (IRuntimeType)v));
            var gvls = LoadGvls(folder, typeParser);
            var pous = LoadPous(folder, typeParser);
            return new(gvls, pous, types);

            static ImmutableArray<CompiledGlobalVariableList> LoadGvls(DirectoryInfo folder, RuntimeTypeParser parser)
            {
                var gvls = ImmutableArray.CreateBuilder<CompiledGlobalVariableList>();
                foreach (var file in folder.GetFiles($"*.{GvlEnding}"))
                {
                    using var stream = file.OpenRead();
                    var gvl = Xml.XmlGlobalVariableList.Parse(stream, parser);
                    gvls.Add(gvl);
                }

                return gvls.ToImmutable();
            }

            static ImmutableArray<CompiledPou> LoadPous(DirectoryInfo folder, RuntimeTypeParser parser)
            {
                var pous = ImmutableArray.CreateBuilder<CompiledPou>();
                foreach (var file in folder.GetFiles($"*.{PouEnding}"))
                {
                    using var stream = file.OpenRead();
                    var pou = Xml.XmlCompiledPou.Parse(stream, parser);
                    pous.Add(pou);
                }

                return pous.ToImmutable();
            }
            static ImmutableArray<RuntimeTypeStructured> LoadTypes(DirectoryInfo folder)
            {
                List<Xml.XmlCompiledType> typeTable = new();
                foreach (var file in folder.GetFiles($"*.{TypeEnding}"))
                {
                    using var stream = file.OpenRead();
                    var pou = Xml.XmlCompiledType.Load(stream);
                    typeTable.Add(pou);
                }
                return XmlCompiledType.ConvertTypes(typeTable);
            }
        }
    }
}
