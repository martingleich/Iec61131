using System;

namespace Runtime.IR
{
	public sealed class Jump : IStatement
	{
		public readonly Label Target;

		public Jump(Label target)
		{
			Target = target ?? throw new ArgumentNullException(nameof(target));
		}

		public int? Execute(Runtime runtime) => Target.StatementId;
		public override string ToString() => $"    jump to {Target.Name}";
	}
}
