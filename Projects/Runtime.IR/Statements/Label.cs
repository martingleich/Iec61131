using Superpower;
using Superpower.Parsers;
using System;

namespace Runtime.IR.Statements
{
	public sealed class Label : IStatement
	{
		public readonly string Name;
		public int StatementId;

		public Label(string name, int statementId)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			StatementId = statementId;
		}
		public Label(string name) : this(name, -1)
		{
		}

		public void SetStatement(int statementId)
		{
			StatementId = statementId;
		}

		public int? Execute(Runtime runtime) => null;
		public override string ToString() => $"label {Name}";
		public static readonly TextParser<Label> ReferenceParser =
			from arg in Span.NonWhiteSpace
			select new Label(arg.ToStringValue());
		public static readonly TextParser<IStatement> StatementParser =
			from _label in Span.EqualTo("label").ThenIgnore(Span.WhiteSpace).IgnoreThen(ReferenceParser)
			select (IStatement)_label;
	}
}
