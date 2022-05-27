using Runtime.IR;
using System;
using Xunit;

namespace CompilerTests.CodeGenTests
{
    public static class StatementChecker
    {
        public static readonly Action<IStatement> Any = st => { };
        public static readonly Action<IStatement> Comment = st => Assert.IsType<Runtime.IR.Statements.Comment>(st);
        public static Action<IStatement> String(string value) => st => Assert.Equal(value, st.ToString());
        public static readonly Action<IStatement> Return = st => Assert.IsType<Runtime.IR.Statements.Return>(st);
    }
}
