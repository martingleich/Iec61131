using System.IO;
using StandardLibraryExtensions;
using System.Collections.Immutable;

namespace Runtime.IR
{
    public sealed record CompiledModule(ImmutableArray<CompiledGlobalVariableList> GlobalVariableLists, ImmutableArray<CompiledPou> Pous)
    {
        public void WriteToDictionary(DirectoryInfo path)
        {
            path.Create();
            foreach (var gvl in GlobalVariableLists)
            {
                var file = path.FileInfo($"{gvl.Name}.gvl.ir.xml");
                using var stream = file.OpenWrite();
                Xml.XmlGlobalVariableList.ToXml(gvl, stream);
            }
            foreach (var pou in Pous)
            {
                var file = path.FileInfo($"{pou.Id.Name}.pou.ir.xml");
                using var stream = file.OpenWrite();
                Parser.ToXml(pou, stream);
            }
        }
    }
}
