using Runtime.IR;
using System;
using Xunit;

namespace CompilerTests.CodeGenTests
{
    public class CodeGenExpressionTests
    {
        private static Action<VariableTable.StackVariable> StackVariable(string name, ushort offset) => v =>
        {
            Assert.Equal(name, v.Name);
            Assert.Equal(offset, v.StackOffset.Offset);
        };
        [Fact]
        public void AddInts()
        {
            var project = BindHelper.NewProject
                .AddFunction("foo", "VAR_INPUT y : INT; END_VAR", "VAR x := y + 2;")
                .CompilerProject;
            ErrorHelper.ExactlyMessages()(project.AllMessages);
            var compiledModule = project.GenerateCode();
            var foo = Assert.Single(compiledModule.Pous);
            Assert.Equal(6, foo.StackUsage);
            Assert.Collection(foo.VariableTable.Variables,
                StackVariable("y", 0),
                StackVariable("x", 2));
            Assert.Collection(foo.Code,
                StatementChecker.Comment,
                StatementChecker.String("copy2 2 to stack4"),
                StatementChecker.String("call __SYSTEM::ADD_INT(stack0, stack4) => stack2"),
                StatementChecker.Return);
        }
        [Fact]
        public void AddConstantInts_Twice()
        {
            var project = BindHelper.NewProject
                .AddFunction("foo", "VAR_INPUT z : INT; END_VAR", "VAR x := z + 2; VAR y := z + 3;")
                .CompilerProject;
            ErrorHelper.ExactlyMessages()(project.AllMessages);
            var compiledModule = project.GenerateCode();
            var foo = Assert.Single(compiledModule.Pous);
            Assert.Equal(8, foo.StackUsage);
            Assert.Collection(foo.VariableTable.Variables,
                StackVariable("z", 0),
                StackVariable("x", 2),
                StackVariable("y", 4));
            Assert.Collection(foo.Code,
                StatementChecker.Comment,
                StatementChecker.String("copy2 2 to stack6"),
                StatementChecker.String("call __SYSTEM::ADD_INT(stack0, stack6) => stack2"),
                StatementChecker.Comment,
                StatementChecker.String("copy2 3 to stack6"),
                StatementChecker.String("call __SYSTEM::ADD_INT(stack0, stack6) => stack4"),
                StatementChecker.Return);
        }
    }
}
