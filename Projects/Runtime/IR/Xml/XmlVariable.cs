using Runtime.IR.RuntimeTypes;
using Superpower;

namespace Runtime.IR.Xml
{
    [System.Xml.Serialization.XmlType("variable")]
    public sealed class XmlVariable
    {
        [System.Xml.Serialization.XmlAttribute("isLocal")]
        public bool IsLocal;
        [System.Diagnostics.CodeAnalysis.AllowNull]
        [System.Xml.Serialization.XmlAttribute("name")]
        public string Name;
        [System.Xml.Serialization.XmlAttribute("offset")]
        public ushort LocalOffset;
        [System.Diagnostics.CodeAnalysis.AllowNull]
        [System.Xml.Serialization.XmlAttribute("type")]
        public string Type;

        public static XmlVariable FromVariable(VariableTable.StackVariable arg)
        {
            return new XmlVariable()
            {
                IsLocal = arg.IsLocal,
                Name = arg.Name,
                LocalOffset = arg.StackOffset.Offset,
                Type = arg.Type.Name
            };
        }
        public VariableTable.StackVariable ToVariable()
        {
            var type = IRuntimeType.Parser.Parse(Type);
            var offset = new LocalVarOffset(LocalOffset);
            if (IsLocal)
                return new VariableTable.LocalStackVariable(Name, offset, type);
            else
                return new VariableTable.ArgStackVariable(Name, offset, type);
        }
    }
}
