using Superpower;
using Superpower.Parsers;
using System;

namespace Runtime.IR.Statements
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

		public int? Execute(RTE runtime)
		{
			var control = runtime.LoadBOOL(Control);
			return control ? null : Target.StatementId;
		}
		public override string ToString() => $"if not {Control} jump to {Target.Name}";
		public static readonly TextParser<IStatement> Parser =
			from _control in Span.EqualTo("if not").ThenIgnore(Span.WhiteSpace).IgnoreThen(LocalVarOffset.Parser).ThenIgnore(Span.WhiteSpace)
			from _label in Span.EqualTo("jump to").ThenIgnore(Span.WhiteSpace).IgnoreThen(Label.ReferenceParser)
			select (IStatement)new JumpIfNot(_control, _label);
	}
}
