using System;
using System.Collections.Immutable;

namespace Runtime.IR
{
    public sealed class CompiledPou
	{
		public readonly PouId Id;
		public readonly ImmutableArray<CompiledArgument> InputArgs;
		public readonly ImmutableArray<CompiledArgument> OutputArgs;
		public readonly ImmutableArray<IStatement> Code;
		public readonly int StackUsage;

		// Optional data
		public BreakpointMap? BreakpointMap { get; init; }
		public string? OriginalPath { get; init; }
		public VariableTable? VariableTable { get; init; }

		public static CompiledPou Action(PouId id, int stackUsage, params IStatement[] code) => new(id, stackUsage, ImmutableArray<CompiledArgument>.Empty, ImmutableArray<CompiledArgument>.Empty, code.ToImmutableArray());
		public CompiledPou(
			PouId id,
			int stackUsage,
			ImmutableArray<CompiledArgument> inputArgs,
			ImmutableArray<CompiledArgument> outputArgs,
			ImmutableArray<IStatement> code)
		{
			if (stackUsage < 0)
				throw new ArgumentException($"{nameof(stackUsage)}({stackUsage}) must be non-negative.");
			Id = id;
			Code = code;
			InputArgs = inputArgs;
			OutputArgs = outputArgs;
			StackUsage = stackUsage;
		}
		public override string ToString() => $"{Id}";
	}

    public record struct CompiledArgument(LocalVarOffset Offset, Type Type)
    {
    }
}
