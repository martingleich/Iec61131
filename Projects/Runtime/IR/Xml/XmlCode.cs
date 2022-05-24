using StandardLibraryExtensions;
using Superpower;
using System;
using System.Collections.Immutable;

namespace Runtime.IR.Xml
{
    public sealed class XmlCode
    {
        [System.Xml.Serialization.XmlAttribute("encoding")]
        [System.Diagnostics.CodeAnalysis.AllowNull]
        public string Encoding;
        [System.Xml.Serialization.XmlText]
        [System.Diagnostics.CodeAnalysis.AllowNull]
        public string Text;

        public static XmlCode FromStatements(ImmutableArray<IStatement> statements) => new XmlCode()
        {
            Encoding = "text",
            Text = Environment.NewLine + statements.DelimitWith(Environment.NewLine) + Environment.NewLine
        };
        public ImmutableArray<IStatement> ToCode()
        {
            if (Encoding != "text")
                throw new InvalidOperationException();
            return CodeParser.Parser.Parse(Text);
        }
    }
}
