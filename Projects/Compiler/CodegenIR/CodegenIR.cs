using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Compiler.Types;
using Runtime.IR;
using IR = Runtime.IR;

namespace Compiler.CodegenIR
{
    public sealed partial class CodegenIR
	{
		public readonly BreakpointMapBuilder BreakpointFactory = new();
		public readonly RuntimeTypeFactoryFromType RuntimeTypeFactory;
		public readonly GeneratorT Generator;
        public readonly GlobalVariableAllocationTable? GlobalVariableAllocationTable;

        public static PouId PouIdFromSymbol(FunctionVariableSymbol variable)
			=> new(variable.UniqueName.ToString().ToUpperInvariant());
		public static PouId PouIdFromSymbol(ICallableTypeSymbol callableSymbol)
			=> new(callableSymbol.UniqueName.ToString().ToUpperInvariant());
		public static IR.Type TypeFromIType(IType type) => new(type.LayoutInfo.Size);

        public static CompiledPou GenerateCode(
			RuntimeTypeFactoryFromType runtimeTypeFactory,
			GlobalVariableAllocationTable globals,
			SourceMap? sourceMap,
			BoundPou toCompile)
        {
            var codegen = new CodegenIR(toCompile, globals, runtimeTypeFactory);
            codegen.CompileInitials(toCompile.LocalVariables);
            codegen.CompileStatement(toCompile.BoundBody.Value);
            var sourceFile = sourceMap?.GetFile(toCompile.CallableSymbol.DeclaringSpan.Start.File);
			var id = PouIdFromSymbol(toCompile.CallableSymbol);
            var compiledPou = codegen.GetCompiledPou(sourceFile, id);
            return compiledPou;
        }

		public static CompiledPou GenerateGvlInitializer(
			RuntimeTypeFactoryFromType runtimeTypeFactory,
			PouId id,
			ImmutableArray<KeyValuePair<MemoryLocation, ILiteralValue>> values)
		{
			var codegen = new CodegenIR(null, null, runtimeTypeFactory);
			codegen.CompileInitials(values);
			codegen.Generator.IL(IR.Statements.Return.Instance);
			var compiledPou = codegen.GetCompiledPou(null, id);
			return compiledPou;
		}
        public static CompiledPou GenerateAssignment(
			RuntimeTypeFactoryFromType runtimeTypeFactory,
			PouId id,
			MemoryLocation dst,
			IBoundExpression expression)
        {
			var codegen = new CodegenIR(null, null, runtimeTypeFactory);
			codegen.CompileAssignment(dst, expression);
			codegen.Generator.IL(IR.Statements.Return.Instance);
			var compiledPou = codegen.GetCompiledPou(null, id);
			return compiledPou;
        }

		private CodegenIR(
			BoundPou? pou,
			GlobalVariableAllocationTable? globalVariableAllocationTable,
			RuntimeTypeFactoryFromType runtimeTypeFactory)
		{
			RuntimeTypeFactory = runtimeTypeFactory;
            GlobalVariableAllocationTable = globalVariableAllocationTable;

			_stackAllocator = StackAllocator.Create(this, pou);

			Generator = new(this);
			_loadLiteralValue = new(this);
			_loadVariableExpressionVisitor = new(this);
			_loadValueExpressionVisitor = new(this);
			_statementVisitor = new(this);
			_variableAddressableVisitor = new(this);
			_addressableVisitor = new(this);
        }

		private CompiledPou GetCompiledPou(SourceMap.SingleFile? sourceMap, PouId id)
		{
			var statments = Generator.GetStatements();
            var breakpointMap = sourceMap != null ? BreakpointFactory.ToBreakpointMap(sourceMap) : null;
			var variableTable = _stackAllocator.GetVariableTable();
            return new(
                id,
                _stackAllocator.TotalMemory,
                _stackAllocator.InputArgs,
                _stackAllocator.OutputArgs,
                statments)
            {
				BreakpointMap = breakpointMap,
				OriginalPath = sourceMap?.FullPath,
				VariableTable = variableTable
            };
		}

		private void CompileInitials(OrderedSymbolSet<LocalVariableSymbol> localVariables)
		{
			foreach (var local in localVariables)
			{
				if (local.InitialValue is IBoundExpression initial)
				{
					var target = Generator.LocalVariable(local);
					var value = initial.Accept(_loadValueExpressionVisitor, target);
					if(value != target)
                        Generator.LocalVariable(local).Assign(this, value);
				}
			}
		}
		private void CompileInitials(ImmutableArray<KeyValuePair<MemoryLocation, ILiteralValue>> values)
		{
			var location = _stackAllocator.AllocTemp(IR.Type.Pointer);
			foreach (var value in values)
			{
                Generator.IL(new IR.Statements.WriteValue(
                    IR.Expressions.LiteralExpression.FromMemoryLocation(value.Key),
					location,
                    IR.Type.Pointer.Size));
                Generator.IL(new IR.Statements.WriteDerefValue(
                    value.Value.Accept(_loadLiteralValue).GetExpression(),
					location,
                    TypeFromIType(value.Value.Type).Size));
			}
		}
		private void CompileAssignment(MemoryLocation location, IBoundExpression value)
		{
			var locationVar = _stackAllocator.AllocTemp(IR.Type.Pointer);
            Generator.IL(new IR.Statements.WriteValue(
                IR.Expressions.LiteralExpression.FromMemoryLocation(location),
                locationVar,
                IR.Type.Pointer.Size));
			var readableValue = value.Accept(_loadValueExpressionVisitor, null);
            Generator.IL(new IR.Statements.WriteDerefValue(
				readableValue.GetExpression(),
                locationVar,
                TypeFromIType(value.Type).Size));
		}
	}
}
