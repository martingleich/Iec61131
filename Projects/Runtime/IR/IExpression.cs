using Runtime.IR.Expressions;
using Superpower;

namespace Runtime.IR
{
	public interface IExpression
	{
		void LoadTo(RTE runtime, MemoryLocation location, int size);
		public static readonly TextParser<IExpression> Parser = Parse.OneOf(
				DerefExpression.Parser,
				LoadValueExpression.Parser,
				LiteralExpression.Parser,
				AddressExpression.Parser);
	}
}
