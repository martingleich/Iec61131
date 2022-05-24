using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Runtime.IR.Xml
{
    public sealed class XmlVariables
    {
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull("table")]
        public static List<XmlStackVariable>? FromVariableTable(VariableTable? table)
        {
            if (table == null)
                return null;
            return table.Variables.Select(XmlStackVariable.FromVariable).ToList();
        }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull("variables")]
        public static VariableTable? ToTable(List<XmlStackVariable>? variables)
        {
            if(variables == null)
                return null;
            var variables2 = variables.Select(v => v.ToVariable()).ToImmutableArray();
            return new(variables2);
        }
    }
}
