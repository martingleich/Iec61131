using Superpower;
using Superpower.Parsers;
using System;

namespace Runtime.IR.Statements
{
	public sealed class Jump : IStatement
	{
		public readonly Label Target;

		public Jump(Label target)
		{
			Target = target ?? throw new ArgumentNullException(nameof(target));
		}

		public int? Execute(RTE runtime) => Target.StatementId;
		public override string ToString() => $"jump to {Target.Name}";
		public static readonly TextParser<IStatement> Parser =
			from _label in Span.EqualTo("jump to").ThenIgnore(Span.WhiteSpace).IgnoreThen(Label.ReferenceParser)
			select (IStatement)new Jump(_label);
        public bool ContainsStatementReference => true;
	}
}
