using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Compiler;
using Compiler.Types;
using IR = Runtime.IR;

namespace OfflineCompiler
{
	public sealed partial class CodegenIR
	{
		private sealed class LoadValueExpressionVisitor : IBoundExpression.IVisitor<IReadable>
		{
			private readonly CodegenIR CodeGen;

			public LoadValueExpressionVisitor(CodegenIR codeGen)
			{
				CodeGen = codeGen ?? throw new ArgumentNullException(nameof(codeGen));
			}

			private GeneratorT Generator => CodeGen.Generator;

			public IReadable Visit(LiteralBoundExpression literalBoundExpression) => new JustReadable(literalBoundExpression.Value.Accept(LoadLiteralValueVisitor.Instance));
			public IReadable Visit(SizeOfTypeBoundExpression sizeOfTypeBoundExpression) => new JustReadable(IR.LiteralExpression.Signed32(sizeOfTypeBoundExpression.Type.LayoutInfo.Size));
			public IReadable Visit(VariableBoundExpression variableBoundExpression) => variableBoundExpression.Variable.Accept(CodeGen._loadVariableExpressionVisitor);
			public IReadable Visit(ImplicitEnumToBaseTypeCastBoundExpression implicitEnumCastBoundExpression) => implicitEnumCastBoundExpression.Value.Accept(this);
			public IReadable Visit(BinaryOperatorBoundExpression binaryOperatorBoundExpression)
			{
				var left = CodeGen.LoadValueAsVariable(binaryOperatorBoundExpression.Left);
				var right = CodeGen.LoadValueAsVariable(binaryOperatorBoundExpression.Right);
				return Generator.IL_SimpleCall(binaryOperatorBoundExpression.Function, left, right);
			}
			public IReadable Visit(UnaryOperatorBoundExpression unaryOperatorBoundExpression)
			{
				var arg = CodeGen.LoadValueAsVariable(unaryOperatorBoundExpression.Value);
				return Generator.IL_SimpleCall(unaryOperatorBoundExpression.Function, arg);
			}
			public IReadable Visit(ImplicitCastBoundExpression implicitArithmeticCaseBoundExpression)
			{
				var arg = CodeGen.LoadValueAsVariable(implicitArithmeticCaseBoundExpression.Value);
				return Generator.IL_SimpleCall(implicitArithmeticCaseBoundExpression.CastFunction, arg);
			}
			public IReadable Visit(ArrayIndexAccessBoundExpression arrayIndexAccessBoundExpression)
				=> CodeGen.LoadAddressable(arrayIndexAccessBoundExpression).ToReadable(CodeGen);
			public IReadable Visit(FieldAccessBoundExpression fieldAccessBoundExpression)
				=> CodeGen.LoadAddressable(fieldAccessBoundExpression).ToReadable(CodeGen);
			public IReadable Visit(PointerIndexAccessBoundExpression pointerIndexAccessBoundExpression)
				=> CodeGen.LoadAddressable(pointerIndexAccessBoundExpression).ToReadable(CodeGen);
			public IReadable Visit(DerefBoundExpression derefBoundExpression)
				=> CodeGen.LoadAddressable(derefBoundExpression).ToReadable(CodeGen);

			public IReadable Visit(ImplicitPointerTypeCastBoundExpression implicitPointerTypeCaseBoundExpression) => implicitPointerTypeCaseBoundExpression.Value.Accept(this);
			public IReadable Visit(ImplicitAliasToBaseTypeCastBoundExpression aliasToBaseTypeCastBoundExpression) => aliasToBaseTypeCastBoundExpression.Value.Accept(this);
			public IReadable Visit(ImplicitAliasFromBaseTypeCastBoundExpression implicitAliasFromBaseTypeCastBoundExpression) => implicitAliasFromBaseTypeCastBoundExpression.Value.Accept(this);
			public IReadable Visit(PointerDiffrenceBoundExpression pointerDiffrenceBoundExpression)
			{
				var left = CodeGen.LoadValueAsVariable(pointerDiffrenceBoundExpression.Left);
				var right = CodeGen.LoadValueAsVariable(pointerDiffrenceBoundExpression.Right);
				return CodeGen.Generator.IL_SimpleCall(IR.Type.Pointer, new IR.PouId("__SYSTEM::SUB_POINTER"), left, right);
			}

			public IReadable Visit(PointerOffsetBoundExpression pointerOffsetBoundExpression)
			{
				var left = CodeGen.LoadValueAsVariable(pointerOffsetBoundExpression.Left);
				var right = CodeGen.LoadValueAsVariable(pointerOffsetBoundExpression.Right);
				return CodeGen.Generator.IL_SimpleCall(IR.Type.Pointer, new IR.PouId("__SYSTEM::ADD_POINTER"), left, right);
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

			public IReadable Visit(CallBoundExpression callBoundExpression)
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

				var returnType = calleeInfo.ReturnParam is ParameterVariableSymbol returnParam ? CodegenIR.TypeFromIType(returnParam.Type) : IR.Type.Bits0;
				var returnVar = CodeGen.Generator.DeclareTemp(returnType);
				CodeGen.Generator.IL(new IR.StaticCall(
					CodegenIR.PouIdFromSymbol(staticCallee),
					inputs.Select(x => x.Offset).ToImmutableArray(),
					intermediateOutputs.Prepend(returnVar).Select(x => x.Offset).ToImmutableArray()));

				for (int i = 0; i < finalOutputs.Length; ++i)
				{
					if (finalOutputs[i] is IWritable finalOutput)
					{
						// We must reassign the output.
						finalOutput.Assign(CodeGen, intermediateOutputs[i]);
					}
				}

				return returnVar;
			}

			public IReadable Visit(InitializerBoundExpression initializerBoundExpression)
			{
				var var = CodeGen.Generator.DeclareTemp(TypeFromIType(initializerBoundExpression.Type));
				foreach (var elem in initializerBoundExpression.Elements)
				{
					var value = CodeGen.LoadValueAsVariable(elem.Value);
					if (elem is InitializerBoundExpression.ABoundElement.ArrayElement arrayElem)
					{
						var type = (ArrayType)initializerBoundExpression.Type;
						var idx = CodeGen.Generator.DeclareTemp(IR.Type.Bits32);
						idx.Assign(CodeGen, new JustReadable(IR.LiteralExpression.Signed32(arrayElem.Index.Value)));
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
						var index = CodeGen.Generator.DeclareTemp(IR.Type.Bits32);
						var indexPtr = CodeGen.Generator.DeclareTemp(IR.Type.Pointer, index.ToPointerValue(CodeGen));
						var arrayPtr = new PointerVariableAddressable(CodeGen.Generator.DeclareTemp(IR.Type.Pointer, var.ToPointerValue(CodeGen)), type.BaseType.LayoutInfo.Size);
						var initialValue = CodeGen.Generator.DeclareTemp(IR.Type.Bits32, new JustReadable(IR.LiteralExpression.Signed32(0)));
						var upperBound = CodeGen.Generator.DeclareTemp(IR.Type.Bits32, new JustReadable(IR.LiteralExpression.Signed32(type.ElementCount)));
						var step = CodeGen.Generator.DeclareTemp(IR.Type.Bits32, new JustReadable(IR.LiteralExpression.Signed32(0)));
						CodeGen._statementVisitor.GenerateForLoop(
							null,
							"DINT",
							v =>
							{
								var element = arrayPtr.GetElementAddressable(CodeGen, new ElementAddressable.Element.PointerIndex(index, type.BaseType.LayoutInfo.Size), type.BaseType.LayoutInfo.Size);
								element.ToWritable(CodeGen).Assign(CodeGen, value);
							},
							indexPtr,
							initialValue,
							upperBound,
							step);
					}
					else
					{
						throw new NotSupportedException();
					}
				}

				return var;
			}

			public IReadable Visit(ImplicitDiscardBoundExpression implicitDiscardBoundExpression)
			{
				implicitDiscardBoundExpression.Value.Accept(CodeGen._loadValueExpressionVisitor);
				return new JustReadable(IR.NullExpression.Instance);
			}
			public IReadable Visit(ImplicitErrorCastBoundExpression implicitErrorCastBoundExpression) => throw new InvalidOperationException();
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
			var value = expression.Accept(_loadValueExpressionVisitor);
			return LoadValueAsVariable(TypeFromIType(expression.Type), value);
		}
		private readonly VariableAddressableVisitor _variableAddressableVisitor;
	}
}
