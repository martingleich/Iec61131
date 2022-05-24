using System.IO;
using StandardLibraryExtensions;
using System.Collections.Immutable;

namespace Runtime.IR
{
    public sealed record CompiledModule(ImmutableArray<CompiledGlobalVariableList> GlobalVariableLists, ImmutableArray<CompiledPou> Pous)
    {
        private const string GvlEnding = "gvl.ir.xml";
        private const string PouEnding = "pou.ir.xml";
        public void WriteToDictionary(DirectoryInfo path)
        {
            path.Create();
            foreach (var gvl in GlobalVariableLists)
            {
                var file = path.FileInfo($"{gvl.Name}.{GvlEnding}");
                using var stream = file.OpenWrite();
                Xml.XmlGlobalVariableList.ToXml(gvl, stream);
            }
            foreach (var pou in Pous)
            {
                var file = path.FileInfo($"{pou.Id.Name.Split("::")[^1]}.{PouEnding}");
                using var stream = file.OpenWrite();
                Xml.XmlCompiledPou.ToXml(pou, stream);
            }
        }
        public static CompiledModule LoadFromDirectory(DirectoryInfo folder)
        {
            var gvls = LoadGvls(folder);
            var pous = LoadPous(folder);
            return new(gvls, pous);

            static ImmutableArray<CompiledGlobalVariableList> LoadGvls(DirectoryInfo folder)
            {
                var gvls = ImmutableArray.CreateBuilder<CompiledGlobalVariableList>();
                foreach (var file in folder.GetFiles($"*.{GvlEnding}"))
                {
                    using var stream = file.OpenRead();
                    var gvl = Xml.XmlGlobalVariableList.Parse(stream);
                    gvls.Add(gvl);
                }

                return gvls.ToImmutable();
            }

            static ImmutableArray<CompiledPou> LoadPous(DirectoryInfo folder)
            {
                var pous = ImmutableArray.CreateBuilder<CompiledPou>();
                foreach (var file in folder.GetFiles($"*.{PouEnding}"))
                {
                    using var stream = file.OpenRead();
                    var pou = Xml.XmlCompiledPou.Parse(stream);
                    pous.Add(pou);
                }

                return pous.ToImmutable();
            }
        }
    }
}
