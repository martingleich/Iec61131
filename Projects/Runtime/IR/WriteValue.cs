using System;

namespace Runtime.IR
{
	public sealed class WriteValue : IStatement
	{
		public readonly IExpression Value;
		public readonly LocalVarOffset Offset;
		public readonly int Size;

		public WriteValue(IExpression value, LocalVarOffset offset, int size)
		{
			Value = value ?? throw new ArgumentNullException(nameof(value));
			Offset = offset;
			Size = size;
		}

		public int? Execute(Runtime runtime)
		{
			var address = runtime.LoadEffectiveAddress(Offset);
			Value.LoadTo(runtime, address, Size);
			return null;
		}

		public override string ToString() => $"    copy{Size} {Value} to {Offset}";
	}
}
