﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Compiler;
using Compiler.Types;
using IR = Runtime.IR;
using IRExpr = Runtime.IR.Expressions;
using IRStmt = Runtime.IR.Statements;

namespace Compiler.CodegenIR
{
	public sealed partial class CodegenIR
	{
		private sealed class LoadValueExpressionVisitor : IBoundExpression.IVisitor<IReadable, LocalVariable?>
		{
			private readonly CodegenIR CodeGen;

			public LoadValueExpressionVisitor(CodegenIR codeGen)
			{
				CodeGen = codeGen ?? throw new ArgumentNullException(nameof(codeGen));
			}

			private GeneratorT Generator => CodeGen.Generator;

			public IReadable Visit(LiteralBoundExpression literalBoundExpression, LocalVariable? targetVar) => literalBoundExpression.Value.Accept(CodeGen._loadLiteralValue);
			public IReadable Visit(SizeOfTypeBoundExpression sizeOfTypeBoundExpression, LocalVariable? targetVar) => new JustReadable(IRExpr.LiteralExpression.Signed32(sizeOfTypeBoundExpression.Type.LayoutInfo.Size));
			public IReadable Visit(VariableBoundExpression variableBoundExpression, LocalVariable? targetVar) => variableBoundExpression.Variable.Accept(CodeGen._loadVariableExpressionVisitor);
			public IReadable Visit(ImplicitEnumToBaseTypeCastBoundExpression implicitEnumCastBoundExpression, LocalVariable? targetVar) => implicitEnumCastBoundExpression.Value.Accept(this, targetVar);
			public IReadable Visit(BinaryOperatorBoundExpression binaryOperatorBoundExpression, LocalVariable? targetVar)
			{
				var left = CodeGen.LoadValueAsVariable(binaryOperatorBoundExpression.Left);
				var right = CodeGen.LoadValueAsVariable(binaryOperatorBoundExpression.Right);
				return Generator.IL_SimpleCall(targetVar, binaryOperatorBoundExpression.Function, left, right);
			}
			public IReadable Visit(UnaryOperatorBoundExpression unaryOperatorBoundExpression, LocalVariable? targetVar)
			{
				var arg = CodeGen.LoadValueAsVariable(unaryOperatorBoundExpression.Value);
				return Generator.IL_SimpleCall(targetVar, unaryOperatorBoundExpression.Function, arg);
			}
			public IReadable Visit(ImplicitCastBoundExpression implicitArithmeticCaseBoundExpression, LocalVariable? targetVar)
			{
				var arg = CodeGen.LoadValueAsVariable(implicitArithmeticCaseBoundExpression.Value);
				return Generator.IL_SimpleCall(targetVar, implicitArithmeticCaseBoundExpression.CastFunction, arg);
			}
			public IReadable Visit(ArrayIndexAccessBoundExpression arrayIndexAccessBoundExpression, LocalVariable? targetVar)
				=> CodeGen.LoadAddressable(arrayIndexAccessBoundExpression).ToReadable(CodeGen);
			public IReadable Visit(FieldAccessBoundExpression fieldAccessBoundExpression, LocalVariable? targetVar)
				=> CodeGen.LoadAddressable(fieldAccessBoundExpression).ToReadable(CodeGen);
			public IReadable Visit(PointerIndexAccessBoundExpression pointerIndexAccessBoundExpression, LocalVariable? targetVar)
				=> CodeGen.LoadAddressable(pointerIndexAccessBoundExpression).ToReadable(CodeGen);
			public IReadable Visit(DerefBoundExpression derefBoundExpression, LocalVariable? targetVar)
				=> CodeGen.LoadAddressable(derefBoundExpression).ToReadable(CodeGen);

			public IReadable Visit(ImplicitPointerTypeCastBoundExpression implicitPointerTypeCaseBoundExpression, LocalVariable? targetVar) => implicitPointerTypeCaseBoundExpression.Value.Accept(this, targetVar);
			public IReadable Visit(ImplicitAliasToBaseTypeCastBoundExpression aliasToBaseTypeCastBoundExpression, LocalVariable? targetVar) => aliasToBaseTypeCastBoundExpression.Value.Accept(this, targetVar);
			public IReadable Visit(ImplicitAliasFromBaseTypeCastBoundExpression implicitAliasFromBaseTypeCastBoundExpression, LocalVariable? targetVar) => implicitAliasFromBaseTypeCastBoundExpression.Value.Accept(this, targetVar);
			public IReadable Visit(PointerDiffrenceBoundExpression pointerDiffrenceBoundExpression, LocalVariable? targetVar)
			{
				var left = CodeGen.LoadValueAsVariable(pointerDiffrenceBoundExpression.Left);
				var right = CodeGen.LoadValueAsVariable(pointerDiffrenceBoundExpression.Right);
				return CodeGen.Generator.IL_SimpleCall(targetVar, IR.Type.Pointer, new IR.PouId("__SYSTEM::SUB_POINTER"), left, right);
			}

			public IReadable Visit(PointerOffsetBoundExpression pointerOffsetBoundExpression, LocalVariable? targetVar)
			{
				var left = CodeGen.LoadValueAsVariable(pointerOffsetBoundExpression.Left);
				var right = CodeGen.LoadValueAsVariable(pointerOffsetBoundExpression.Right);
				return CodeGen.Generator.IL_SimpleCall(targetVar, IR.Type.Pointer, new IR.PouId("__SYSTEM::ADD_POINTER"), left, right);
			}

			private sealed class CalleeInfo
			{
				private readonly Dictionary<int, int> _idMap;
				private readonly bool _takesSelf;
				public readonly ParameterVariableSymbol? ReturnParam;
				public readonly int InputsCount;
				public readonly int OutputsCount;

				public CalleeInfo(ICallableTypeSymbol type)
				{
					_idMap = new Dictionary<int, int>();
					_takesSelf = type is FunctionBlockSymbol;
					if (_takesSelf)
						InputsCount = 1;
					foreach (var param in type.Parameters)
					{
						if (param.Kind.Equals(ParameterKind.Input) || param.Kind.Equals(ParameterKind.InOut))
							_idMap[param.ParameterId] = InputsCount++;
						else if (param.Kind.Equals(ParameterKind.Output))
						{
							if (param.Name == type.Name)
								ReturnParam = param;
							else
								_idMap[param.ParameterId] = OutputsCount++;
						}
					}
				}

				public bool TakesSelf(out int selfId)
				{
					selfId = 0;
					return _takesSelf;
				}

				public int GetId(ParameterVariableSymbol parameter) => _idMap[parameter.ParameterId];
			}

			public IReadable Visit(CallBoundExpression callBoundExpression, LocalVariable? targetVar)
			{
				var staticCallee = (ICallableTypeSymbol)callBoundExpression.Callee.Type; // Will always succeed for compiling code.
				var calleeInfo = new CalleeInfo(staticCallee);

				var inputs = new LocalVariable[calleeInfo.InputsCount];
				var intermediateOutputs = new LocalVariable[calleeInfo.OutputsCount];
				var finalOutputs = new IWritable?[calleeInfo.OutputsCount];

				foreach (var arg in callBoundExpression.Arguments)
				{
					if (arg.ParameterSymbol.Kind.Equals(ParameterKind.Input))
					{
						var value = CodeGen.LoadValueAsVariable(arg.Value);
						inputs[calleeInfo.GetId(arg.ParameterSymbol)] = value;
					}
					else if (arg.ParameterSymbol.Kind.Equals(ParameterKind.InOut))
					{
						var value = CodeGen.LoadAddressAsVariable(arg.Value);
						inputs[calleeInfo.GetId(arg.ParameterSymbol)] = value;
					}
					else
					{
						var finalOutput = CodeGen.LoadWritable(arg.Value);
						if (!(arg.Parameter is VariableBoundExpression vexpr && vexpr.Variable == arg.ParameterSymbol))
							throw new NotImplementedException("Cannot generate code for function output casts.");
						var id = calleeInfo.GetId(arg.ParameterSymbol);
						if (finalOutput is LocalVariable localVar)
						{
							finalOutputs[id] = null;
							intermediateOutputs[id] = localVar;
						}
						else
						{
							var intermediateOutput = Generator.DeclareTemp(arg.ParameterSymbol.Type);
							intermediateOutputs[id] = intermediateOutput;
							finalOutputs[id] = finalOutput;
						}
					}
				}

				if (calleeInfo.TakesSelf(out var selfId))
					inputs[selfId] = CodeGen.LoadAddressAsVariable(callBoundExpression.Callee);

				IR.Type? maybeReturnType = calleeInfo.ReturnParam is ParameterVariableSymbol returnParam ? TypeFromIType(returnParam.Type) : null;
				LocalVariable returnVariable;
				if (maybeReturnType is IR.Type returnType)
				{
					returnVariable = targetVar ?? CodeGen.Generator.DeclareTemp(returnType);
					intermediateOutputs = intermediateOutputs.Prepend(returnVariable).ToArray();
				}
				else
				{
					returnVariable = targetVar ?? CodeGen.Generator.DeclareTemp(IR.Type.Bits0);
				}
				CodeGen.Generator.IL(new IRStmt.StaticCall(
					PouIdFromSymbol(staticCallee),
					inputs.Select(x => x.Offset).ToImmutableArray(),
					intermediateOutputs.Select(x => x.Offset).ToImmutableArray()));

				for (int i = 0; i < finalOutputs.Length; ++i)
				{
					if (finalOutputs[i] is IWritable finalOutput)
					{
						// We must reassign the output.
						finalOutput.Assign(CodeGen, intermediateOutputs[i]);
					}
				}

				return returnVariable;
			}

			public IReadable Visit(InitializerBoundExpression initializerBoundExpression, LocalVariable? targetVar)
			{
				var var = targetVar ?? CodeGen.Generator.DeclareTemp(TypeFromIType(initializerBoundExpression.Type));
				foreach (var elem in initializerBoundExpression.Elements)
				{
					var value = CodeGen.LoadValueAsVariable(elem.Value);
					if (elem is InitializerBoundExpression.ABoundElement.ArrayElement arrayElem)
					{
						var type = (ArrayType)initializerBoundExpression.Type;
						var idx = CodeGen.Generator.DeclareTemp(IR.Type.Bits32);
						idx.Assign(CodeGen, new JustReadable(IRExpr.LiteralExpression.Signed32(arrayElem.Index.Value)));
						var.GetElementAddressable(CodeGen, new ElementAddressable.Element.ArrayIndex(ImmutableArray.Create(idx), type), TypeFromIType(arrayElem.Value.Type).Size)
							.ToWritable(CodeGen)
							.Assign(CodeGen, value);
					}
					else if (elem is InitializerBoundExpression.ABoundElement.FieldElement fieldElem)
					{
						var.GetElementAddressable(CodeGen, new ElementAddressable.Element.Field(fieldElem.Field), TypeFromIType(fieldElem.Field.Type).Size)
							.ToWritable(CodeGen)
							.Assign(CodeGen, value);
					}
					else if (elem is InitializerBoundExpression.ABoundElement.AllElements)
					{
						var type = (ArrayType)initializerBoundExpression.Type;
						var idx = CodeGen.Generator.DeclareTemp(IR.Type.Bits32);
						var ptr = CodeGen.Generator.DeclareTemp(IR.Type.Pointer);
						var endptr = CodeGen.Generator.DeclareTemp(IR.Type.Pointer);
						var cond = CodeGen.Generator.DeclareTemp(IR.Type.Bits8);
						CodeGen.Generator.IL(new IR.Statements.WriteValue(new IRExpr.LiteralExpression(0), idx.Offset, IR.Type.Bits32.Size));
						CodeGen.Generator.IL(new IR.Statements.WriteValue(
							new ElementAddressable(new IRExpr.AddressExpression.BaseStackVar(var.Offset),
							ImmutableArray.Create<ElementAddressable.Element>(new ElementAddressable.Element.PointerIndex(idx, type.BaseType.LayoutInfo.Size)),
							type.BaseType.LayoutInfo.Size).ToPointerValue(CodeGen).GetExpression(), ptr.Offset, IR.Type.Pointer.Size));
						CodeGen.Generator.IL(new IR.Statements.WriteValue(IRExpr.LiteralExpression.Signed32(type.ElementCount), idx.Offset, IR.Type.Bits32.Size));
						CodeGen.Generator.IL(new IR.Statements.WriteValue(
							new ElementAddressable(new IRExpr.AddressExpression.BaseStackVar(var.Offset),
							ImmutableArray.Create<ElementAddressable.Element>(new ElementAddressable.Element.PointerIndex(idx, type.BaseType.LayoutInfo.Size)),
							type.BaseType.LayoutInfo.Size).ToPointerValue(CodeGen).GetExpression(), endptr.Offset, IR.Type.Pointer.Size));
						CodeGen.Generator.IL(new IR.Statements.WriteValue(IRExpr.LiteralExpression.Signed32(type.BaseType.LayoutInfo.Size), idx.Offset, IR.Type.Bits32.Size));
						var label = CodeGen.Generator.DeclareLabel();
						CodeGen.Generator.IL_Label(label);
						CodeGen.Generator.IL(new IR.Statements.WriteDerefValue(
							value.GetExpression(),
							ptr.Offset,
							type.BaseType.LayoutInfo.Size));
						CodeGen.Generator.IL_SimpleCall(ptr, IR.Type.Pointer, new IR.PouId("__SYSTEM::ADD_POINTER"), ptr, idx);
						CodeGen.Generator.IL_SimpleCall(cond, IR.Type.Pointer, new IR.PouId("__SYSTEM::EQUAL_DINT"), ptr, endptr);
						CodeGen.Generator.IL_Jump_IfNot(cond, label);
					}
					else
					{
						throw new NotSupportedException();
					}
				}

				return var;
			}

			public IReadable Visit(ImplicitDiscardBoundExpression implicitDiscardBoundExpression, LocalVariable? targetVar)
			{
				implicitDiscardBoundExpression.Value.Accept(CodeGen._loadValueExpressionVisitor, targetVar);
				return new JustReadable(IRExpr.NullExpression.Instance);
			}
			public IReadable Visit(ImplicitErrorCastBoundExpression implicitErrorCastBoundExpression, LocalVariable? targetVar) => throw new InvalidOperationException();
		}
		private readonly LoadValueExpressionVisitor _loadValueExpressionVisitor;

		private LocalVariable LoadValueAsVariable(IR.Type type, IReadable value)
		{
			if (value is not LocalVariable variable)
				variable = Generator.DeclareTemp(type, value);
			return variable;
		}
		private LocalVariable LoadValueAsVariable(IBoundExpression expression)
		{
			var value = expression.Accept(_loadValueExpressionVisitor, null);
			return LoadValueAsVariable(TypeFromIType(expression.Type), value);
		}
		private readonly VariableAddressableVisitor _variableAddressableVisitor;
	}
}
