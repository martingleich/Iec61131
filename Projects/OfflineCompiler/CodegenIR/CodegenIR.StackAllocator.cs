using System.Collections.Generic;
using System.Collections.Immutable;
using Compiler;
using Compiler.Types;
using IR = Runtime.IR;

namespace OfflineCompiler
{
	public sealed partial class CodegenIR
	{
		public sealed class StackAllocator
		{
			private readonly Dictionary<int, IR.LocalVarOffset> _stackLocalVarOffsets = new();
			private readonly Dictionary<int, IR.LocalVarOffset> _paramVarOffsets = new();
			private ushort _cursor;
			public readonly IR.LocalVarOffset? ThisVariableOffset;
			public ushort TotalMemory => _cursor;
			public readonly ImmutableArray<(IR.LocalVarOffset, int)> InputArgs;
			public readonly ImmutableArray<(IR.LocalVarOffset, int)> OutputArgs;

			public StackAllocator(ICallableTypeSymbol calleeType)
			{
				if (calleeType is FunctionBlockSymbol)
					ThisVariableOffset = AllocParameter(-1, IR.Type.Pointer);
				else
					ThisVariableOffset = null;

				var inputArgs = ImmutableArray.CreateBuilder<(IR.LocalVarOffset, int)>();
				foreach (var param in calleeType.Parameters)
				{
					IR.Type type;
					if (param.Kind == ParameterKind.Input)
					{
						type = CodegenIR.TypeFromIType(param.Type);
						inputArgs.Add((AllocParameter(param.ParameterId, type), type.Size));
					}
					else if (param.Kind == ParameterKind.InOut)
					{
						type = IR.Type.Pointer;
						inputArgs.Add((AllocParameter(param.ParameterId, type), type.Size));
					}
				}
				InputArgs = inputArgs.ToImmutable();
				var outputArgs = ImmutableArray.CreateBuilder<(IR.LocalVarOffset, int)>();
				foreach (var param in calleeType.Parameters)
				{
					IR.Type type;
					if (param.Kind == ParameterKind.Output)
					{
						type = CodegenIR.TypeFromIType(param.Type);
						outputArgs.Add((AllocParameter(param.ParameterId, type), type.Size));
					}
				}
				OutputArgs = outputArgs.ToImmutable();
			}

			public IR.LocalVarOffset AllocStackLocal(int localId, IR.Type type)
			{
				if (!_stackLocalVarOffsets.TryGetValue(localId, out var offset))
				{
					offset = AllocTemp(type);
					_stackLocalVarOffsets.Add(localId, offset);
				}
				return offset;
			}
			public IR.LocalVarOffset AllocParameter(int paramId, IR.Type type)
			{
				if (!_paramVarOffsets.TryGetValue(paramId, out var offset))
				{
					offset = AllocTemp(type);
					_paramVarOffsets.Add(paramId, offset);
				}
				return offset;
			}
			public IR.LocalVarOffset AllocTemp(IR.Type type)
			{
				var offset = new IR.LocalVarOffset(_cursor);
				_cursor += (ushort)type.Size;
				return offset;
			}
		}
	
		private readonly StackAllocator _stackAllocator;
	}
}
