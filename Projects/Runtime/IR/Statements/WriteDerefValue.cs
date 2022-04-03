using System;

namespace Runtime.IR.Statements
{
	public sealed class WriteDerefValue : IStatement
	{
		public readonly IExpression Value;
		public readonly LocalVarOffset Target;
		public readonly int Size;

		public WriteDerefValue(IExpression value, LocalVarOffset targetOffset, int size)
		{
			Value = value ?? throw new ArgumentNullException(nameof(value));
			Target = targetOffset;
			Size = size;
		}

		public int? Execute(Runtime runtime)
		{
			var address = runtime.LoadPointer(Target);
			Value.LoadTo(runtime, address, Size);
			return null;
		}
		public override string ToString() => $"copy{Size} {Value} to *{Target}";
	}
}
