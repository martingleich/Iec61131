using System;
using System.Collections.Immutable;
using IRExpr = Runtime.IR.Expressions;

namespace Compiler.CodegenIR
{
    public sealed class PointerVariableAddressable : IAddressable
	{
		public readonly LocalVariable Variable;
		public readonly int Size;

		public PointerVariableAddressable(LocalVariable variable, int size)
		{
			Variable = variable ?? throw new ArgumentNullException(nameof(variable));
			Size = size;
		}

		public IReadable ToReadable(CodegenIR codegen) => new Deref(Variable, Size);
		public IWritable ToWritable(CodegenIR codegen) => new Deref(Variable, Size);
		public IReadable ToPointerValue(CodegenIR codegen) => Variable;
		public IAddressable GetElementAddressable(CodegenIR codegen, ElementAddressable.Element element, int derefSize)
			=> new ElementAddressable(new IRExpr.AddressExpression.BaseDerefStackVar(Variable.Offset), ImmutableArray.Create(element), derefSize);
		public override string ToString() => $"{Variable}^";
	}
}
