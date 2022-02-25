using System;

namespace Runtime.IR
{
	public sealed class JumpIfNot : IStatement
	{
		public readonly LocalVarOffset Control;
		public readonly Label Target;

		public JumpIfNot(LocalVarOffset control, Label target)
		{
			Control = control;
			Target = target ?? throw new ArgumentNullException(nameof(target));
		}

		public int? Execute(Runtime runtime)
		{
			var control = runtime.LoadBOOL(Control);
			return control ? Target.StatementId : null;
		}
		public override string ToString() => $"    if not {Control} jump to {Target.Name}";
	}
}
