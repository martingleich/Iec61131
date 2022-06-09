using System;
using System.Collections.Immutable;
using Runtime.IR;
using IR = Runtime.IR;
using IRExpr = Runtime.IR.Expressions;
using IRStmt = Runtime.IR.Statements;

namespace Compiler.CodegenIR
{
    public sealed class LocalVariable : IReadable, IWritable, IAddressable
	{
		public readonly string Id;
		public readonly IR.Type Type;
		public readonly IR.LocalVarOffset Offset;

		public LocalVariable(string id, IR.Type type, IR.LocalVarOffset offset)
		{
			Id = id;
			Type = type;
			Offset = offset;
		}
		public void Assign(CodegenIR codegen, IReadable value)
		{
			codegen.Generator.IL(new IRStmt.WriteValue(
				value.GetExpression(),
				Offset,
				Type.Size));
		}
		public IReadable ToReadable(CodegenIR codegen) => this;
		public IWritable ToWritable(CodegenIR codegen) => this;
		public IReadable ToPointerValue(CodegenIR codegen) => new ElementAddressable(new IRExpr.AddressExpression.BaseStackVar(Offset), Type.Size).ToPointerValue(codegen);
		public IR.IExpression GetExpression() => new IRExpr.LoadValueExpression(Offset);
		public override string ToString() => Id;

		public IAddressable GetElementAddressable(CodegenIR codegen, ElementAddressable.Element element, int derefSize)
			=> new ElementAddressable(new IRExpr.AddressExpression.BaseStackVar(Offset), ImmutableArray.Create(element), derefSize);
	}
}
