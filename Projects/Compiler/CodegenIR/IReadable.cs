using IR = Runtime.IR;

namespace Compiler.CodegenIR
{
    public interface IReadable
	{
		IR.IExpression GetExpression();
	}
}
