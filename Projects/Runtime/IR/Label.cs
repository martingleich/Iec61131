using System;

namespace Runtime.IR
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
	}
}
