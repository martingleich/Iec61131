﻿using StandardLibraryExtensions;
using System;
using System.Collections.Immutable;

namespace Runtime.IR
{
	public sealed class CompiledPou
	{
		public readonly PouId Id;
		public readonly ImmutableArray<(LocalVarOffset, int)> InputArgs;
		public readonly ImmutableArray<(LocalVarOffset, int)> OutputArgs;
		public readonly ImmutableArray<IStatement> Code;
		public readonly int StackUsage;

		public CompiledPou(PouId id, ImmutableArray<IStatement> code, ImmutableArray<(LocalVarOffset, int)> inputArgs, ImmutableArray<(LocalVarOffset, int)> outputArgs, int stackUsage)
		{
			Id = id;
			Code = code;
			InputArgs = inputArgs;
			OutputArgs = outputArgs;
			StackUsage = stackUsage;
		}
		public override string ToString()
		{
			return @$"id: {Id}
inputs: {InputArgs.DelimitWith(", ")}
outputs: {OutputArgs.DelimitWith(", ")}
stackusage: {StackUsage}
{Code.DelimitWith(Environment.NewLine)}";
		}
	}
}
