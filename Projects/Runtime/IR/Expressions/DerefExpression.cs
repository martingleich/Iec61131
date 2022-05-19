using Superpower;
using Superpower.Parsers;

namespace Runtime.IR.Expressions
{
	public sealed class DerefExpression : IExpression
	{
		public readonly LocalVarOffset Address;

		public DerefExpression(LocalVarOffset location)
		{
			Address = location;
		}

		public void LoadTo(RTE runtime, MemoryLocation location, int size)
		{
			var l1 = runtime.LoadPointer(Address);
			runtime.Copy(l1, location, size);
		}

		public override string ToString() => $"*{Address}";
		public static readonly TextParser<IExpression> Parser =
			from _value in Span.EqualTo("*").IgnoreThen(LocalVarOffset.Parser)
		    select (IExpression)new DerefExpression(_value);
	}
}
