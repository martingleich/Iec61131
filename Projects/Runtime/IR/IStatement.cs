using Runtime.IR.Statements;
using Superpower;

namespace Runtime.IR
{
	public interface IStatement
	{
		int? Execute(RTE runtime);
		public static readonly TextParser<IStatement> Parser = Parse.OneOf(
			WriteValue.Parser.Try(), // WriteValue is a prefix of staticCall, so we only try it.
			StaticCall.Parser,
			Return.Parser,
			Comment.Parser,
			Jump.Parser,
			JumpIfNot.Parser,
			Label.StatementParser);
        bool ContainsStatementReference { get; }
    }
}
