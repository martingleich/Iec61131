using Superpower;
using Superpower.Parsers;

namespace Runtime.IR.Statements
{
	public sealed class Return : IStatement
	{
		public static readonly Return Instance = new();
		public int? Execute(Runtime runtime) => runtime.Return();

		public override string ToString() => "return";
		public static readonly TextParser<IStatement> Parser =
			Span.EqualTo("return").IgnoreThen(Parse.Return((IStatement)Instance));
	}
}
