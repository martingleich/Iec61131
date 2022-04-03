using StandardLibraryExtensions;
using Superpower;
using Superpower.Parsers;
using System.Collections.Immutable;

namespace Runtime.IR.Statements
{
	public sealed class StaticCall : IStatement
	{
		public readonly PouId Callee;
		public readonly ImmutableArray<LocalVarOffset> Inputs;
		public readonly ImmutableArray<LocalVarOffset> Outputs;

		public StaticCall(PouId callee, ImmutableArray<LocalVarOffset> inputs, ImmutableArray<LocalVarOffset> outputs)
		{
			Callee = callee;
			Inputs = inputs;
			Outputs = outputs;
		}

		public int? Execute(Runtime runtime) => runtime.Call(Callee, Inputs, Outputs);
		public override string ToString()
		{
			var args = Inputs.DelimitWith(", ");
			var results = Outputs.DelimitWith(", ");
			return $"call {Callee}({args}) => {results}";
		}
		public static readonly TextParser<IStatement> Parser =
			from _callee in Span.EqualTo("call").ThenIgnore(Span.WhiteSpace).IgnoreThen(Span.Except("(")).Select(str => new PouId(str.ToStringValue()))
			from _inputs in LocalVarOffset.Parser.CommaSeperatedList().Between(Span.EqualTo("("), Span.EqualTo(")"))
			from _1 in Span.EqualTo("=>").SuroundOptionalWhitespace()
			from _outputs in LocalVarOffset.Parser.CommaSeperatedList()
			select (IStatement)new StaticCall(_callee, _inputs, _outputs);
	}
}
