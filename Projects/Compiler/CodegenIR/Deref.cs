using IR = Runtime.IR;
using IRExpr = Runtime.IR.Expressions;
using IRStmt = Runtime.IR.Statements;

namespace Compiler.CodegenIR
{
    public sealed class Deref : IReadable, IWritable
	{
		private readonly LocalVariable Pointer;
		private readonly int Size;

		public Deref(LocalVariable pointer, int size)
		{
			Pointer = pointer;
			Size = size;
		}

		public void Assign(CodegenIR codegen, IReadable value)
		{
			var irValue = value.GetExpression();
			codegen.Generator.IL(new IRStmt.WriteDerefValue(irValue, Pointer.Offset, Size));
		}

		public IR.IExpression GetExpression() => new IRExpr.DerefExpression(Pointer.Offset);
		public override string ToString() => $"{Pointer}^";
	}
}
