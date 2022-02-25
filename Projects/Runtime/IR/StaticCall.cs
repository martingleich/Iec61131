using StandardLibraryExtensions;
using System.Collections.Immutable;

namespace Runtime.IR
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
			return $"    call {Callee}({args}) => {results}";
		}
	}
}
