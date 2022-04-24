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

			private readonly ImmutableArray<IR.VariableTable.StackVariable>.Builder _debugVariables = ImmutableArray.CreateBuilder<IR.VariableTable.StackVariable>();
            public IR.VariableTable GetVariableTable() => new(_debugVariables.ToImmutable());

            private readonly SystemScope SystemScope;

            public StackAllocator(ICallableTypeSymbol calleeType, SystemScope systemScope)
			{
                this.SystemScope = systemScope;

				if (calleeType is FunctionBlockSymbol)
					ThisVariableOffset = _paramVarOffsets[-1] = AllocTemp(IR.Type.Pointer);
				else
					ThisVariableOffset = null;

				var inputArgs = ImmutableArray.CreateBuilder<(IR.LocalVarOffset, int)>();
				foreach (var param in calleeType.Parameters)
				{
					if (param.Kind == ParameterKind.Input || param.Kind == ParameterKind.InOut)
					{
						var (offset, type) = AllocParameter(param);
						inputArgs.Add((offset, type.Size));
					}
				}
				InputArgs = inputArgs.ToImmutable();
				var outputArgs = ImmutableArray.CreateBuilder<(IR.LocalVarOffset, int)>();
				foreach (var param in calleeType.Parameters)
				{
					if (param.Kind == ParameterKind.Output)
					{
						var (offset, type) = AllocParameter(param);
						outputArgs.Add((offset, type.Size));
					}
				}
				OutputArgs = outputArgs.ToImmutable();
            }

			public (IR.LocalVarOffset, IR.Type) AllocStackLocal(ILocalVariableSymbol localVariable)
			{
				var irType = TypeFromIType(localVariable.Type);
				if (!_stackLocalVarOffsets.TryGetValue(localVariable.LocalId, out var offset))
				{
					offset = AllocTemp(irType);
					_stackLocalVarOffsets.Add(localVariable.LocalId, offset);
					_debugVariables.Add(new IR.VariableTable.LocalStackVariable(localVariable.Name.Original, offset, GetDebugType(localVariable.Type)));
				}

				return (offset, irType);
			}

            private IR.IDebugType GetDebugType(IType type)
            {
				if(TypeRelations.IsIdentical(type, SystemScope.BuiltInTypeTable.Int))
					return IR.DebugTypeINT.Instance;
				else
					return new IR.DebugTypeUnknown(type.Code);
            }

            public (IR.LocalVarOffset, IR.Type) AllocParameter(ParameterVariableSymbol symbol)
			{
                var type = symbol.Kind.Equals(ParameterKind.InOut) ? IR.Type.Pointer : TypeFromIType(symbol.Type);
				if (!_paramVarOffsets.TryGetValue(symbol.ParameterId, out var offset))
				{
					offset = AllocTemp(type);
					_paramVarOffsets.Add(symbol.ParameterId, offset);
					_debugVariables.Add(new IR.VariableTable.ArgStackVariable(symbol.Name.Original, offset, GetDebugType(symbol.Type)));
				}
				return (offset, type);
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
