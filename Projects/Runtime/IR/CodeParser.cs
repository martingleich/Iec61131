using Superpower;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Runtime.IR
{
    using Statements;
    public static class CodeParser
    {
        private static ImmutableArray<IStatement> FixLabels(ImmutableArray<IStatement> statements)
        {
            var offsets = new Dictionary<string, int>();
            int id = 0;
            foreach (var st in statements)
            {
                if (st is Label label)
                {
                    label.SetStatement(id);
                    offsets[label.Name] = id;
                }
                else if (st is Jump jump)
                {
                    jump.Target.SetStatement(offsets[jump.Target.Name]);
                }
                else if (st is JumpIfNot jumpIfNot)
                {
                    jumpIfNot.Target.SetStatement(offsets[jumpIfNot.Target.Name]);
                }
                ++id;
            }
            return statements;
        }
        private static TextParser<ImmutableArray<IStatement>> CreateCodeParser()
        {
            var statements = IStatement.Parser.ThenIgnore(ParserUtils.OptionalWhitespace).ManyImmutable();
            return statements.Select(FixLabels);
        }

        public static readonly TextParser<ImmutableArray<IStatement>> Parser = CreateCodeParser().SuroundOptionalWhitespace();
    }
}
