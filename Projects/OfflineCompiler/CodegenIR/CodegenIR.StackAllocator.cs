using System.Collections.Generic;
using System.Collections.Immutable;
using Compiler;
using Compiler.Types;
using Runtime.IR.RuntimeTypes;
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


            public readonly ImmutableArray<IR.CompiledArgument> InputArgs;
			public readonly ImmutableArray<IR.CompiledArgument> OutputArgs;

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

				var inputArgs = ImmutableArray.CreateBuilder<IR.CompiledArgument>();
				foreach (var param in calleeType.Parameters)
				{
					if (param.Kind == ParameterKind.Input || param.Kind == ParameterKind.InOut)
					{
						var arg = AllocParameter(param);
						inputArgs.Add(arg);
					}
				}
				InputArgs = inputArgs.ToImmutable();
				var outputArgs = ImmutableArray.CreateBuilder<IR.CompiledArgument>();
				foreach (var param in calleeType.Parameters)
				{
					if (param.Kind == ParameterKind.Output)
					{
						var arg = AllocParameter(param);
						outputArgs.Add(arg);
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

            private IRuntimeType GetDebugType(IType type)
            {
				if(TypeRelations.IsIdentical(type, SystemScope.BuiltInTypeTable.SInt))
					return RuntimeTypeSINT.Instance;
				else if(TypeRelations.IsIdentical(type, SystemScope.BuiltInTypeTable.Int))
					return RuntimeTypeINT.Instance;
				else if(TypeRelations.IsIdentical(type, SystemScope.BuiltInTypeTable.DInt))
					return RuntimeTypeDINT.Instance;
				else if(TypeRelations.IsIdentical(type, SystemScope.BuiltInTypeTable.LInt))
					return RuntimeTypeLINT.Instance;
				else if(TypeRelations.IsIdentical(type, SystemScope.BuiltInTypeTable.Real))
					return RuntimeTypeREAL.Instance;
				else if(TypeRelations.IsIdentical(type, SystemScope.BuiltInTypeTable.LReal))
					return RuntimeTypeLREAL.Instance;
				else if(TypeRelations.IsIdentical(type, SystemScope.BuiltInTypeTable.Bool))
					return RuntimeTypeBOOL.Instance;
				else
					return new RuntimeTypeUnknown(type.Code);
            }

            public IR.CompiledArgument AllocParameter(ParameterVariableSymbol symbol)
			{
                var type = symbol.Kind.Equals(ParameterKind.InOut) ? IR.Type.Pointer : TypeFromIType(symbol.Type);
				if (!_paramVarOffsets.TryGetValue(symbol.ParameterId, out var offset))
				{
					offset = AllocTemp(type);
					_paramVarOffsets.Add(symbol.ParameterId, offset);
					_debugVariables.Add(new IR.VariableTable.ArgStackVariable(symbol.Name.Original, offset, GetDebugType(symbol.Type)));
				}
				return new (offset, type);
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
