using System.Collections.Immutable;
using Runtime.IR;
using IR = Runtime.IR;
using IRExpr = Runtime.IR.Expressions;

namespace Compiler.CodegenIR
{
    public sealed class GlobalVariable : IAddressable
	{
		public readonly string Id;
		public readonly MemoryLocation Location;
		public readonly int Size;

		public GlobalVariable(string id, MemoryLocation location, int size)
		{
			Id = id;
			Location = location;
			Size = size;
		}

		private Deref Deref(CodegenIR codegen)
		{
			var ptr = ToPointerValue(codegen);
			var tmp = codegen.Generator.DeclareTemp(IR.Type.Pointer, ptr);
			return new Deref(tmp, Size);
		}

		public IReadable ToPointerValue(CodegenIR codegen) => new JustReadable(IRExpr.LiteralExpression.FromMemoryLocation(Location));
		public IReadable ToReadable(CodegenIR codegen) => Deref(codegen);
		public IWritable ToWritable(CodegenIR codegen) => Deref(codegen);
		public override string ToString() => Id;

		public IAddressable GetElementAddressable(CodegenIR codegen, ElementAddressable.Element element, int derefSize)
		{
			var ptrValue = ToPointerValue(codegen);
			var baseVar = codegen.Generator.DeclareTemp(IR.Type.Pointer, ptrValue);
			return new ElementAddressable(new IRExpr.AddressExpression.BaseDerefStackVar(baseVar.Offset), ImmutableArray.Create(element), derefSize);
		}
	}
}
