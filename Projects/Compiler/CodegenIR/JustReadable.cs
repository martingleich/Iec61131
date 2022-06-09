using IR = Runtime.IR;

namespace Compiler.CodegenIR
{
    public sealed record JustReadable(IR.IExpression Expression) : IReadable
	{
		public IR.IExpression GetExpression() => Expression;
		public override string? ToString() => Expression.ToString();
	}
}
