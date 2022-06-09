namespace Compiler.CodegenIR
{
    public interface IWritable
	{
		void Assign(CodegenIR codegen, IReadable value);
	}
}
