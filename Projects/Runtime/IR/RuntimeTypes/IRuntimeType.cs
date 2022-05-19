using Superpower;
using System.Collections.Immutable;
using System.Linq;

namespace Runtime.IR.RuntimeTypes
{
    public interface IRuntimeType
    {
        string Name { get; }
        string ReadValue(MemoryLocation location, RTE runtime);

        private static readonly ImmutableArray<IRuntimeType> RuntimeTypes = ImmutableArray.Create<IRuntimeType>(
                RuntimeTypeSINT.Instance,
                RuntimeTypeSINT.Instance,
                RuntimeTypeINT.Instance,
                RuntimeTypeDINT.Instance,
                RuntimeTypeLINT.Instance,
                RuntimeTypeREAL.Instance,
                RuntimeTypeLREAL.Instance);
        public static readonly TextParser<IRuntimeType> ParserDefinite = input =>
        {
            foreach (var runtimeType in RuntimeTypes)
            {
                if (input.EqualsValueIgnoreCase(runtimeType.Name))
                {
                    return Superpower.Model.Result.Value(runtimeType, input, Superpower.Model.TextSpan.Empty);
                }
            }
            return Superpower.Model.Result.Empty<IRuntimeType>(input);
        };
        public static readonly TextParser<IRuntimeType> Parser = ParserDefinite.Or(Superpower.Parsers.Span.NonWhiteSpace.Select(span => (IRuntimeType)new RuntimeTypeUnknown(span.ToStringValue())));
    }

    public interface IEquatableRuntimeType : IRuntimeType
    {
        bool Equals(MemoryLocation a, MemoryLocation b, RTE runtime);
    }
    public interface IComparableRuntimeType : IRuntimeType
    {
        int Compare(MemoryLocation a, MemoryLocation b, RTE runtime);
    }
}
