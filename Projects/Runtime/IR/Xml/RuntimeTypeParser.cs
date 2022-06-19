using Runtime.IR.RuntimeTypes;
using Runtime.IR.Xml;
using Superpower;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Runtime.IR
{
    public class RuntimeTypeParser
    {
        private readonly ImmutableDictionary<string, XmlCompiledType> _unparsedTypes;
        private readonly Dictionary<string, IRuntimeType?> _parsedTypes;
        private readonly TextParser<RuntimeTypeArray> _arrayParser;

        public RuntimeTypeParser(ImmutableDictionary<string, XmlCompiledType> unparsedTypes) : this(unparsedTypes, new())
        {
        }
        public RuntimeTypeParser(Dictionary<string, IRuntimeType> parsedTypes) : this(ImmutableDictionary<string, XmlCompiledType>.Empty, parsedTypes)
        {
        }
        public RuntimeTypeParser() : this(ImmutableDictionary<string, XmlCompiledType>.Empty, new())
        {
        }

        private RuntimeTypeParser(ImmutableDictionary<string, XmlCompiledType> unparsedTypes, Dictionary<string, IRuntimeType> parsedTypes)
        {
            _parsedTypes = parsedTypes!;
            _unparsedTypes = unparsedTypes;
            _arrayParser = MakeParser(src => Superpower.Model.Result.Value(Parse(src.ToStringValue()), src, Superpower.Model.TextSpan.Empty));
        }

        public RuntimeTypeStructured Convert(XmlCompiledType type)
        {
            return type.ToType(Parse);
        }
        private static readonly ImmutableArray<IRuntimeType> RuntimeTypes = ImmutableArray.Create<IRuntimeType>(
                RuntimeTypeSINT.Instance,
                RuntimeTypeSINT.Instance,
                RuntimeTypeINT.Instance,
                RuntimeTypeDINT.Instance,
                RuntimeTypeLINT.Instance,
                RuntimeTypeREAL.Instance,
                RuntimeTypeLREAL.Instance);

        public static IRuntimeType? TryParseBuiltIn(string text)
        {
            foreach (var type in RuntimeTypes)
            {
                if (text.Equals(type.Name, StringComparison.InvariantCultureIgnoreCase))
                    return type;
            }
            return null;
        }
        public IRuntimeType Parse(string text)
        {
            if (TryParseBuiltIn(text) is IRuntimeType builtInType)
                return builtInType;
            var maybeArray = _arrayParser.TryParse(text);
            if (maybeArray.HasValue)
                return maybeArray.Value;
            if (!_parsedTypes.TryGetValue(text, out var structuredType))
            {
                if (_unparsedTypes.TryGetValue(text, out var xmlType))
                {
                    _parsedTypes[text] = null;
                    _parsedTypes[text] = structuredType = Convert(xmlType);
                }
                else
                    _parsedTypes[text] = structuredType = new RuntimeTypeUnknown(text, 0);
            }
            if (structuredType == null)
                throw new InvalidOperationException();
            return structuredType;
        }
        private static TextParser<RuntimeTypeArray> MakeParser(TextParser<IRuntimeType> baseTypeParser) =>
            from _1 in Superpower.Parsers.Span.EqualToIgnoreCase("ARRAY[")
            from dimensions in Superpower.Parse.Chain(Superpower.Parsers.Span.EqualTo(",").SuroundOptionalWhitespace(), ArrayTypeRange.Parser.SuroundOptionalWhitespace().Select(ImmutableArray.Create), (_, a, b) => a.AddRange(b))
            from _2 in Superpower.Parsers.Span.EqualToIgnoreCase("] OF ")
            from baseType in baseTypeParser
            select new RuntimeTypeArray(dimensions, baseType);
    }
}
