using Superpower;

namespace Runtime.IR.Expressions
{
	public sealed class LoadValueExpression : IExpression
	{
		public readonly LocalVarOffset Offset;

		public LoadValueExpression(LocalVarOffset offset)
		{
			Offset = offset;
		}

		public void LoadTo(RTE runtime, MemoryLocation location, int size)
		{
			var pointer = runtime.LoadEffectiveAddress(Offset);
			runtime.Copy(pointer, location, size);
		}
		public override string ToString() => $"{Offset}";
		public static readonly TextParser<IExpression> Parser =
			from _value in LocalVarOffset.Parser
			select (IExpression)new LoadValueExpression(_value);
	}
}
