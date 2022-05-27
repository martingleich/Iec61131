using System;
using Xunit;

namespace CompilerTests.CodeGenTests
{
    public class CodeGenGvlTests
    {
        private static Action<Runtime.IR.CompiledGlobalVariableList.Variable> CheckGlobalVar(string name, ushort offset, string typeName) => var =>
        {
            Assert.Equal(name, var.Name);
            Assert.Equal(offset, var.Offset);
            Assert.Equal(typeName, var.Type.Name);
        };
        private static Action<Runtime.IR.CompiledGlobalVariableList> CheckGlobalVarList(string name, ushort area, int size, params Action<Runtime.IR.CompiledGlobalVariableList.Variable>[] variables) => gvl =>
        {
            Assert.Equal(name, gvl.Name);
            Assert.Equal(area, gvl.Area);
            Assert.Equal(size, gvl.Size);
            Assert.Collection(gvl.VariableTable.Value, variables);
        };
        [Fact]
        public void Empty()
        {
            var project = BindHelper.NewProject
                .AddGVL("MyGVL", "")
                .CompilerProject;
            ErrorHelper.ExactlyMessages()(project.AllMessages);
            var compiledModule = project.GenerateCode();
            Assert.Collection(compiledModule.GlobalVariableLists,
                CheckGlobalVarList("MyGVL", 2, 0));
        }
        [Fact]
        public void SingleElement()
        {
            var project = BindHelper.NewProject
                .AddGVL("MyGVL", "VAR_GLOBAL x : INT; END_VAR")
                .CompilerProject;
            ErrorHelper.ExactlyMessages()(project.AllMessages);
            var compiledModule = project.GenerateCode();
            Assert.Collection(compiledModule.GlobalVariableLists,
                CheckGlobalVarList("MyGVL", 2, 2,
                    CheckGlobalVar("x", 0, "INT")));
        }
        [Fact]
        public void TwoElements()
        {
            var project = BindHelper.NewProject
                .AddGVL("MyGVL", "VAR_GLOBAL x : INT; y : BOOL; END_VAR")
                .CompilerProject;
            ErrorHelper.ExactlyMessages()(project.AllMessages);
            var compiledModule = project.GenerateCode();
            Assert.Collection(compiledModule.GlobalVariableLists,
                CheckGlobalVarList("MyGVL", 2, 3,
                    CheckGlobalVar("x", 0, "INT"),
                    CheckGlobalVar("y", 2, "BOOL")));
        }
        [Fact]
        public void TwoElements_ReorderByAlignment()
        {
            var project = BindHelper.NewProject
                .AddGVL("MyGVL", "VAR_GLOBAL x : BOOL; y : INT; END_VAR")
                .CompilerProject;
            ErrorHelper.ExactlyMessages()(project.AllMessages);
            var compiledModule = project.GenerateCode();
            Assert.Collection(compiledModule.GlobalVariableLists,
                CheckGlobalVarList("MyGVL", 2, 3,
                    CheckGlobalVar("y", 0, "INT"),
                    CheckGlobalVar("x", 2, "BOOL")));
        }
        [Fact]
        public void TwoGvls()
        {
            var project = BindHelper.NewProject
                .AddGVL("MyGVL", "VAR_GLOBAL x : BOOL; END_VAR")
                .AddGVL("MyGVL2", "VAR_GLOBAL y : INT; END_VAR")
                .CompilerProject;
            ErrorHelper.ExactlyMessages()(project.AllMessages);
            var compiledModule = project.GenerateCode();
            Assert.Collection(compiledModule.GlobalVariableLists,
                CheckGlobalVarList("MyGVL", 2, 1,
                    CheckGlobalVar("x", 0, "BOOL")),
                CheckGlobalVarList("MyGVL2", 3, 2,
                    CheckGlobalVar("y", 0, "INT")));
        }
        [Fact]
        public void InitialValue()
        {
            var project = BindHelper.NewProject
                .AddGVL("MyGVL", "VAR_GLOBAL x : INT := 12345; y : BOOL := TRUE; END_VAR")
                .CompilerProject;
            ErrorHelper.ExactlyMessages()(project.AllMessages);
            var compiledModule = project.GenerateCode();
            Assert.Collection(compiledModule.GlobalVariableLists[0].Initializer!.Code,
                StatementChecker.String("copy4 131072 to stack0"),
                StatementChecker.String("copy2 12345 to *stack0"),
                StatementChecker.String("copy4 131074 to stack0"),
                StatementChecker.String("copy1 255 to *stack0"),
                StatementChecker.Return);
        }
    }
}
