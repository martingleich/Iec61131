using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Compiler.Types;
using IR = Runtime.IR;

namespace Compiler.CodegenIR
{
    public sealed partial class CodegenIR
	{
		public sealed class StackAllocator
		{
			private readonly Dictionary<int, IR.LocalVarOffset> _stackLocalVarOffsets = new();
			private readonly Dictionary<int, IR.LocalVarOffset> _paramVarOffsets = new();
			private ushort _cursor;
			private ushort _maxStackLocal;
			public readonly IR.LocalVarOffset? ThisVariableOffset;
			public ushort TotalMemory { get; private set; }

            public readonly ImmutableArray<IR.CompiledArgument> InputArgs;
			public readonly ImmutableArray<IR.CompiledArgument> OutputArgs;

			private readonly ImmutableArray<IR.VariableTable.StackVariable>.Builder _debugVariables = ImmutableArray.CreateBuilder<IR.VariableTable.StackVariable>();
            public IR.VariableTable GetVariableTable() => new(_debugVariables.ToImmutable());

            private readonly CodegenIR CodeGen;

            private StackAllocator(
				CodegenIR codeGen,
				ICallableTypeSymbol calleeType)
			{
                CodeGen = codeGen ?? throw new ArgumentNullException(nameof(codeGen));

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

			public (IR.LocalVarOffset, IR.Type) GetLocalInfo(ILocalVariableSymbol symbol)
			{
				var irType = TypeFromIType(symbol.Type);
				var offset = _stackLocalVarOffsets[symbol.LocalId];
				return (offset, irType);
			}
			private (IR.LocalVarOffset, IR.Type) AllocLocal(ILocalVariableSymbol symbol)
			{
				var irType = TypeFromIType(symbol.Type);
                var offset = AllocTemp(irType);
                _stackLocalVarOffsets.Add(symbol.LocalId, offset);
                if (_cursor > _maxStackLocal)
                    _maxStackLocal = _cursor;
                var runtimeType = CodeGen.RuntimeTypeFactory.GetRuntimeType(symbol.Type);
                _debugVariables.Add(new IR.VariableTable.LocalStackVariable(symbol.Name.Original, offset, runtimeType));
				return (offset, irType);
			}

            internal void FreeAllTemporaries()
            {
				_cursor = _maxStackLocal; // Reset cursor to max stack local offset.
            }

            public IR.CompiledArgument GetParameterInfo(ParameterVariableSymbol symbol)
			{
                var type = symbol.Kind.Equals(ParameterKind.InOut) ? IR.Type.Pointer : TypeFromIType(symbol.Type);
				var offset = _paramVarOffsets[symbol.ParameterId];
				return new (offset, type);
			}
			private IR.CompiledArgument AllocParameter(ParameterVariableSymbol symbol)
			{
                var type = symbol.Kind.Equals(ParameterKind.InOut) ? IR.Type.Pointer : TypeFromIType(symbol.Type);
                var offset = AllocTemp(type);
                _paramVarOffsets.Add(symbol.ParameterId, offset);
                if (_cursor > _maxStackLocal)
                    _maxStackLocal = _cursor;
                var runtimeType = CodeGen.RuntimeTypeFactory.GetRuntimeType(symbol.Type);
                _debugVariables.Add(new IR.VariableTable.ArgStackVariable(symbol.Name.Original, offset, runtimeType));
				return new (offset, type);
			}

			public IR.LocalVarOffset AllocTemp(IR.Type type)
			{
				var offset = new IR.LocalVarOffset(_cursor);
				_cursor += (ushort)type.Size;
				TotalMemory = Math.Max(_cursor, TotalMemory);
				return offset;
			}

            public static StackAllocator Create(CodegenIR codegen, BoundPou toCompile)
            {
				var result = new StackAllocator(codegen, toCompile.CallableSymbol);
				var allocator = new InlineVariableVisitor(result);
				foreach (var local in toCompile.LocalVariables)
					result.GetLocalInfo(local);
				toCompile.BoundBody.Value.Accept(allocator);
				return result;
            }

            private sealed class InlineVariableVisitor : IBoundStatement.IVisitor
            {
                private readonly StackAllocator _stackAllocator;

                public InlineVariableVisitor(StackAllocator stackAllocator)
                {
                    _stackAllocator = stackAllocator ?? throw new ArgumentNullException(nameof(stackAllocator));
                }

                public void Visit(InitVariableBoundStatement initVariableBoundStatement)
                {
                    _stackAllocator.AllocLocal(initVariableBoundStatement.LeftSide);
                }

                #region Recurse
                public void Visit(SequenceBoundStatement sequenceBoundStatement)
                {
                    foreach (var st in sequenceBoundStatement.Statements)
                        st.Accept(this);
                }

                public void Visit(IfBoundStatement ifBoundStatement)
                {
                    foreach (var branch in ifBoundStatement.Branches)
                        branch.Body.Accept(this);
                }

                public void Visit(WhileBoundStatement whileBoundStatement)
                {
                    whileBoundStatement.Body.Accept(this);
                }

                public void Visit(ForLoopBoundStatement forLoopBoundStatement)
                {
                    forLoopBoundStatement.Body.Accept(this);
                }
                #endregion

                #region Empty
                public void Visit(ExitBoundStatement exitBoundStatement) { }
                public void Visit(ContinueBoundStatement continueBoundStatement) { }
                public void Visit(ReturnBoundStatement returnBoundStatement) { }
                public void Visit(ExpressionBoundStatement expressionBoundStatement) { }
                public void Visit(AssignBoundStatement assignToExpressionBoundStatement) { }
                #endregion
            }
        }
	
		private readonly StackAllocator _stackAllocator;
	}
}
