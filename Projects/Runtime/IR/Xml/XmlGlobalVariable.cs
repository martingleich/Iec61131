using Runtime.IR.RuntimeTypes;
using Superpower;

namespace Runtime.IR.Xml
{
    [System.Xml.Serialization.XmlType("globalvariable")]
    public sealed class XmlGlobalVariable
    {
        [System.Diagnostics.CodeAnalysis.AllowNull]
        [System.Xml.Serialization.XmlAttribute("name")]
        public string Name;
        [System.Diagnostics.CodeAnalysis.AllowNull]
        [System.Xml.Serialization.XmlAttribute("offset")]
        public ushort Offset;
        [System.Diagnostics.CodeAnalysis.AllowNull]
        [System.Xml.Serialization.XmlAttribute("type")]
        public string Type;

        public static XmlGlobalVariable FromObject(CompiledGlobalVariableList.Variable variable) => new()
        {
            Name = variable.Name,
            Offset = variable.Offset,  
            Type = variable.Type.Name,
        };
        public CompiledGlobalVariableList.Variable ToObject(RuntimeTypeParser parser) => new(
            Name,
            Offset,
            parser.Parse(Type));
    }
}
