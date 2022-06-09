namespace Compiler.CodegenIR
{
    public interface IAddressable
	{
		IReadable ToReadable(CodegenIR codegen);
		IWritable ToWritable(CodegenIR codegen);
		IReadable ToPointerValue(CodegenIR codegen);

		IAddressable GetElementAddressable(CodegenIR codegen, ElementAddressable.Element element, int derefSize);
	}
}
