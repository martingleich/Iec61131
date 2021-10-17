using System;
using System.Collections.Immutable;
using System.Linq;
using Compiler.Messages;
using Compiler.Scopes;
using Compiler.Types;
using StandardLibraryExtensions;

namespace Compiler
{
	public sealed partial class ExpressionBinder : IExpressionSyntax.IVisitor<IBoundExpression, IType?>
	{
		private readonly MessageBag MessageBag;
		private readonly LiteralTokenBinder _literalTokenBinder;
		private readonly IScope Scope;
		private SystemScope SystemScope => Scope.SystemScope;

		private ExpressionBinder(IScope scope, MessageBag messageBag)
		{
			Scope = scope ?? throw new ArgumentNullException(nameof(scope));
			MessageBag = messageBag ?? throw new ArgumentNullException(nameof(messageBag));
			_literalTokenBinder = new LiteralTokenBinder(this);
		}

		public static IBoundExpression Bind(IExpressionSyntax syntax, IScope scope, MessageBag messageBag, IType? targetType)
			=> syntax.Accept(new ExpressionBinder(scope, messageBag), targetType);

		private IBoundExpression ImplicitCast(IBoundExpression boundValue, IType? targetType)
		{
			if (targetType == null || TypeRelations.IsIdentical(boundValue.Type, targetType))
			{
				return boundValue;
			}
			else if (TypeRelations.IsNullType(targetType))
			{
				return new ImplicitDiscardBoundExpression(boundValue);
			}
			else if (TypeRelations.IsAliasType(boundValue.Type, out var sourceAliasTypeSymbol))
			{
				var cast = new ImplicitAliasToBaseTypeCastBoundExpression(boundValue, sourceAliasTypeSymbol.AliasedType);
				return ImplicitCast(cast, targetType);
			}
			else if (TypeRelations.IsAliasType(targetType, out var aliasTypeSymbol))
			{
				var castBaseValue = ImplicitCast(boundValue, aliasTypeSymbol.AliasedType);
				return new ImplicitAliasFromBaseTypeCastBoundExpression(castBaseValue, aliasTypeSymbol);
			}
			else if (TypeRelations.IsEnumType(boundValue.Type, out _))
			{
				var enumValue = new ImplicitEnumToBaseTypeCastBoundExpression(boundValue);
				return ImplicitCast(enumValue, targetType);
			}
			else if (TypeRelations.IsPointerType(targetType, out var targetPointerType) && TypeRelations.IsPointerType(boundValue.Type, out _))
			{
				return new ImplicitPointerTypeCastBoundExpression(boundValue, targetPointerType);
			}
			else if (TypeRelations.IsBuiltInType(targetType, out var builtInTarget) && TypeRelations.IsBuiltInType(boundValue.Type, out var builtInSource) && SystemScope.IsAllowedArithmeticImplicitCast(builtInSource, builtInTarget))
			{
				return new ImplicitArithmeticCastBoundExpression(boundValue, targetType);
			}

			MessageBag.Add(new TypeIsNotConvertibleMessage(boundValue.Type, targetType, boundValue.OriginalNode.SourcePosition));
			return new ImplicitErrorCastBoundExpression(boundValue, targetType);
		}

		private (IBoundExpression, PointerType)? TryImplicitCastToPointer(IBoundExpression boundValue)
		{
			var resolved = TypeRelations.ResolveAlias(boundValue.Type);
			if (TypeRelations.IsPointerType(resolved, out var pointerType))
				return (ImplicitCast(boundValue, pointerType), pointerType);
			else
				return null;
		}

		public IBoundExpression Visit(LiteralExpressionSyntax literalExpressionSyntax, IType? context)
		{
			// Literals are typed depending on the context, we must resolve the alias to do this correctly! 
			var realType = TypeRelations.ResolveAliasNullable(context);
			// "0" can be converted to every pointer type
			if (TypeRelations.IsPointerType(realType, out var targetPointerType) && literalExpressionSyntax.TokenValue is IntegerLiteralToken intLiteral && intLiteral.Value.IsZero)
				return ImplicitCast(new LiteralBoundExpression(literalExpressionSyntax, new NullPointerLiteralValue(targetPointerType)), context);
			else
				return ImplicitCast(literalExpressionSyntax.TokenValue.Accept(_literalTokenBinder, realType), context);
		}

		public IBoundExpression Visit(BinaryOperatorExpressionSyntax binaryOperatorExpressionSyntax, IType? context)
		{
			var boundLeft = binaryOperatorExpressionSyntax.Left.Accept(this, null);
			var boundRight = binaryOperatorExpressionSyntax.Right.Accept(this, null);
			if (TryVisitPointerArithmetic(binaryOperatorExpressionSyntax, boundLeft, boundRight) is IBoundExpression pointerArithemticResult)
			{
				return ImplicitCast(pointerArithemticResult, context);
			}
			else
			{
				// Perform naive overload resolution
				// This function only works if the arguments for the target function both have the same type.
				// i.e. ADD_DInt must take two DINTs, and so on.
				var commonArgType = SystemScope.GetSmallestCommonImplicitCastType(boundLeft.Type, boundRight.Type);
				var realCommonArgType = TypeRelations.ResolveAliasNullable(commonArgType);
				OperatorFunction? operatorFunction;
				if (TypeRelations.IsBuiltInType(realCommonArgType, out var b))
					operatorFunction = SystemScope.BuiltInFunctionTable.TryGetBinaryOperatorFunction(binaryOperatorExpressionSyntax.TokenOperator, b);
				else
					operatorFunction = default;

				if (!operatorFunction.HasValue)
				{
					MessageBag.Add(new CannotPerformArithmeticOnTypesMessage(binaryOperatorExpressionSyntax.TokenOperator.SourcePosition, boundLeft.Type, boundRight.Type));
					var function = FunctionSymbol.CreateError(binaryOperatorExpressionSyntax.TokenOperator.SourcePosition,
						ImplicitName.ErrorBinaryOperator(boundLeft.Type.Code, boundRight.Type.Code, SystemScope.BuiltInFunctionTable.GetBinaryOperatorFunctionName(binaryOperatorExpressionSyntax.TokenOperator)));
					operatorFunction = new OperatorFunction(function, false);
				}

				var returnType = operatorFunction.Value.Symbol.ReturnType;
				var castedLeft = ImplicitCast(boundLeft, realCommonArgType);
				var castedRight = ImplicitCast(boundRight, realCommonArgType);
				IBoundExpression binaryOperatorExpression = new BinaryOperatorBoundExpression(binaryOperatorExpressionSyntax, returnType, castedLeft, castedRight, operatorFunction.Value.Symbol);
				if (operatorFunction.Value.IsGenericReturn)
					binaryOperatorExpression = ImplicitCast(binaryOperatorExpression, commonArgType);
				return ImplicitCast(binaryOperatorExpression, context);
			}
		}

		private IBoundExpression? TryVisitPointerArithmetic(
			BinaryOperatorExpressionSyntax binaryOperatorExpressionSyntax,
			IBoundExpression boundLeft,
			IBoundExpression boundRight)
		{
			var baseLeftType = TypeRelations.ResolveAlias(boundLeft.Type);
			var baseRightType = TypeRelations.ResolveAlias(boundRight.Type);
			if (TypeRelations.IsPointerType(baseLeftType, out _) && TypeRelations.IsPointerType(baseRightType, out _) && binaryOperatorExpressionSyntax.TokenOperator is MinusToken)
			{
				var castedLeft = ImplicitCast(boundLeft, baseLeftType);
				var castedRight = ImplicitCast(boundRight, baseRightType);
				return new PointerDiffrenceBoundExpression(binaryOperatorExpressionSyntax, castedLeft, castedRight, SystemScope.PointerDiffrenceType);
			}
			else if (TypeRelations.IsPointerType(baseLeftType, out var ptrLeft) && TypeRelations.IsBuiltInType(baseRightType, out var bright) && bright.IsInt && binaryOperatorExpressionSyntax.TokenOperator is MinusToken or PlusToken)
			{
				var castedLeft = ImplicitCast(boundLeft, baseLeftType);
				var castedRight = ImplicitCast(boundRight, SystemScope.PointerDiffrenceType);
				return ImplicitCast(new PointerOffsetBoundExpression(
					binaryOperatorExpressionSyntax,
					castedLeft,
					castedRight,
					ptrLeft), boundLeft.Type);

			}
			else if (TypeRelations.IsBuiltInType(baseLeftType, out var lright) && lright.IsInt && TypeRelations.IsPointerType(baseRightType, out var ptrRight) && binaryOperatorExpressionSyntax.TokenOperator is PlusToken)
			{
				var castedLeft = ImplicitCast(boundLeft, SystemScope.PointerDiffrenceType);
				var castedRight = ImplicitCast(boundLeft, baseLeftType);
				return ImplicitCast(new PointerOffsetBoundExpression(
					binaryOperatorExpressionSyntax,
					castedRight,
					castedLeft,
					ptrRight), boundRight.Type);
			}
			else
			{
				return null;
			}
		}

		public IBoundExpression Visit(UnaryOperatorExpressionSyntax unaryOperatorExpressionSyntax, IType? context)
		{
			var boundValue = unaryOperatorExpressionSyntax.Value.Accept(this, null);
			var realBoundType = TypeRelations.ResolveAlias(boundValue.Type);
			OperatorFunction? operatorFunction;
			if (TypeRelations.IsBuiltInType(realBoundType, out var b))
				operatorFunction = SystemScope.BuiltInFunctionTable.TryGetUnaryOperatorFunction(unaryOperatorExpressionSyntax.TokenOperator, b);
			else
				operatorFunction = default;

			if (!operatorFunction.HasValue)
			{
				MessageBag.Add(new CannotPerformArithmeticOnTypesMessage(unaryOperatorExpressionSyntax.TokenOperator.SourcePosition, boundValue.Type));
				var function = FunctionSymbol.CreateError(unaryOperatorExpressionSyntax.TokenOperator.SourcePosition,
					ImplicitName.ErrorUnaryOperator(boundValue.Type.Code, SystemScope.BuiltInFunctionTable.GetUnaryOperatorFunctionName(unaryOperatorExpressionSyntax.TokenOperator)));
				operatorFunction = new OperatorFunction(function, false);
			}

			var returnType = operatorFunction.Value.Symbol.ReturnType;
			IBoundExpression unaryOperatorExpression = new UnaryOperatorBoundExpression(unaryOperatorExpressionSyntax, returnType, boundValue, operatorFunction.Value.Symbol);
			if (operatorFunction.Value.IsGenericReturn)
				unaryOperatorExpression = ImplicitCast(unaryOperatorExpression, boundValue.Type);
			return ImplicitCast(unaryOperatorExpression, context);
		}

		public IBoundExpression Visit(ParenthesisedExpressionSyntax parenthesisedExpressionSyntax, IType? context)
			=> parenthesisedExpressionSyntax.Value.Accept(this, context);

		public IBoundExpression Visit(VariableExpressionSyntax variableExpressionSyntax, IType? context)
		{
			var variable = Scope.LookupVariable(variableExpressionSyntax.Identifier, variableExpressionSyntax.SourcePosition).Extract(MessageBag);
			var boundExpression = new VariableBoundExpression(variableExpressionSyntax, variable);
			return ImplicitCast(boundExpression, context);
		}

		public IBoundExpression Visit(CompoAccessExpressionSyntax compoAccessExpressionSyntax, IType? context)
		{
			var boundLeft = BindLeftCompo(compoAccessExpressionSyntax.LeftSide);
			return ImplicitCast(boundLeft.BindCompo(compoAccessExpressionSyntax, compoAccessExpressionSyntax.TokenIdentifier, this), context);
		}

		public IBoundExpression Visit(DerefExpressionSyntax derefExpressionSyntax, IType? context)
		{
			var value = derefExpressionSyntax.LeftSide.Accept(this, null);
			IBoundExpression castedValue;
			IType baseType;
			if (TryImplicitCastToPointer(value) is (IBoundExpression, PointerType) castResult)
			{
				baseType = castResult.Item2.BaseType;
				castedValue = castResult.Item1;
			}
			else
			{
				MessageBag.Add(new CannotDereferenceTypeMessage(value.Type, derefExpressionSyntax.SourcePosition));
				baseType = value.Type;
				castedValue = value;
			}

			var boundExpression = new DerefBoundExpression(derefExpressionSyntax, castedValue, baseType);
			return ImplicitCast(boundExpression, context);
		}

		public IBoundExpression Visit(IndexAccessExpressionSyntax indexAccessExpressionSyntax, IType? context)
		{
			void CheckIndexCount(int expectedCount, ImmutableArray<IBoundExpression> boundIndices)
			{
				if (expectedCount != boundIndices.Length)
				{
					var sourcePos = expectedCount < boundIndices.Length
						? boundIndices.Skip(expectedCount).SourcePositionHull()
						: boundIndices.Last().OriginalNode.SourcePosition;
					MessageBag.Add(new WrongNumberOfDimensionInIndexMessage(1, boundIndices.Length, sourcePos));
				}
			}

			var boundBase = indexAccessExpressionSyntax.LeftSide.Accept(this, null);
			var castedIndices = indexAccessExpressionSyntax.Indices.Select(idx => ImplicitCast(idx.Accept(this, null), SystemScope.PointerDiffrenceType)).ToImmutableArray();
			var realBaseType = TypeRelations.ResolveAlias(boundBase.Type);
			if (TypeRelations.IsPointerType(realBaseType, out var pointerBaseType))
			{
				CheckIndexCount(1, castedIndices);
				return ImplicitCast(new PointerIndexAccessBoundExpression(indexAccessExpressionSyntax, pointerBaseType.BaseType, castedIndices), context);
			}
			else if (TypeRelations.IsArrayType(realBaseType, out var arrayBaseType))
			{
				CheckIndexCount(arrayBaseType.Ranges.Length, castedIndices);
				return ImplicitCast(new ArrayIndexAccessBoundExpression(indexAccessExpressionSyntax, arrayBaseType.BaseType, castedIndices), context);
			}
			else
			{
				MessageBag.Add(new CannotIndexTypeMessage(boundBase.Type, indexAccessExpressionSyntax.TokenBracketOpen.SourcePosition));
				return ImplicitCast(new ArrayIndexAccessBoundExpression(indexAccessExpressionSyntax, boundBase.Type, castedIndices), context);
			}
		}

		public IBoundExpression Visit(SizeOfExpressionSyntax sizeOfExpressionSyntax, IType? context)
		{
			var type = TypeCompiler.MapSymbolic(Scope, sizeOfExpressionSyntax.Argument, MessageBag);
			return ImplicitCast(new SizeOfTypeBoundExpression(sizeOfExpressionSyntax, type, Scope.SystemScope.Int), context);
		}

		public IBoundExpression Visit(CallExpressionSyntax callExpressionSyntax, IType? context)
		{
			if (callExpressionSyntax.Callee is VariableExpressionSyntax vexpr)
			{
				var function = Scope.LookupFunction(vexpr.Identifier, vexpr.SourcePosition).Extract(MessageBag);
				return ImplicitCast(BindFunctionCall(callExpressionSyntax, function, callExpressionSyntax.Arguments), context);
			}
			else
			{
				MessageBag.Add(new CannotCallSyntaxMessage(callExpressionSyntax.Callee.SourcePosition));
				var function = FunctionSymbol.CreateError(callExpressionSyntax.Callee.SourcePosition);
				return ImplicitCast(BindFunctionCall(callExpressionSyntax, function, callExpressionSyntax.Arguments), context);
			}
		}

		private IBoundExpression BindFunctionCall(ISyntax originalNode, FunctionSymbol function, SyntaxCommaSeparated<CallArgumentSyntax> args)
		{
			// Bind arguments
			if (args.Count != function.ParameterCountWithoutReturn && !function.IsError)
			{
				// Error wrong number of arguments.
				MessageBag.Add(new WrongNumberOfArgumentsMessage(function, args.Count, args.SourcePosition));
			}

			var boundArguments = ImmutableArray.CreateBuilder<FunctionCallBoundExpression.Argument>();
			int nextParamId = 0;
			bool afterExplicit = false;
			foreach (var arg in args)
			{
				ParameterSymbol symbol;
				//	Find assigned argument
				if (arg.ExplicitParameter is ExplicitCallParameterSyntax explicitParameterSyntax)
				{
					if (!function.Parameters.TryGetValue(explicitParameterSyntax.Identifier, out var explicitParameter) ||
						explicitParameterSyntax.Identifier == function.Name) // The implicit output for return is not accessable from the outside
					{
						MessageBag.Add(new ParameterNotFoundMessage(function, explicitParameterSyntax.Identifier, explicitParameterSyntax.TokenIdentifier.SourcePosition));
						symbol = ParameterSymbol.CreateError(explicitParameterSyntax.Identifier, explicitParameterSyntax.TokenIdentifier.SourcePosition);
					}
					else
					{
						symbol = explicitParameter;
						if (!explicitParameter.Kind.MatchesAssignKind(explicitParameterSyntax.ParameterKind))
							MessageBag.Add(new ParameterKindDoesNotMatchAssignMessage(symbol, explicitParameterSyntax.ParameterKind));
					}

					afterExplicit = true;
				}
				else
				{
					if (afterExplicit)
					{
						MessageBag.Add(new CannotUsePositionalParameterAfterExplicitMessage(arg.SourcePosition));
						symbol = ParameterSymbol.CreateError(nextParamId, arg.SourcePosition);
					}
					else if (nextParamId >= function.Parameters.Length)
					{
						symbol = ParameterSymbol.CreateError(nextParamId, arg.SourcePosition);
					}
					else
					{
						symbol = function.Parameters[nextParamId];
					}

					if (!symbol.Kind.Equals(ParameterKind.Input))
						MessageBag.Add(new NonInputParameterMustBePassedExplicit(symbol, arg.SourcePosition));

					++nextParamId;
				}

				boundArguments.Add(BindCallArgument(symbol, arg.Value));
			}

			var boundArgumentsFrozen = boundArguments.ToImmutable();
			CheckForDuplicateParameter(boundArgumentsFrozen);
			return new FunctionCallBoundExpression(originalNode, function, boundArgumentsFrozen);
		}

		private void CheckForDuplicateParameter(ImmutableArray<FunctionCallBoundExpression.Argument> boundArguments)
		{
			foreach (var d in from arg in boundArguments
							  group arg by arg.ParameterSymbol.Name into duplicates
							  where duplicates.MoreThan(1)
							  select duplicates)
			{
				var first = d.First();
				foreach (var d2 in d.Skip(1))
				{
					MessageBag.Add(new ParameterWasAlreadyPassedMessage(first.ParameterSymbol, first.Parameter.OriginalNode.SourcePosition, d2.Parameter.OriginalNode.SourcePosition));
				}
			}
		}

		private FunctionCallBoundExpression.Argument BindCallArgument(ParameterSymbol symbol, IExpressionSyntax arg)
		{
			if (symbol.Kind.Equals(ParameterKind.Input))
			{
				var value = arg.Accept(this, symbol.Type);
				return new(
					symbol,
					new VariableBoundExpression(arg, symbol),
					value);
			}
			else if (symbol.Kind.Equals(ParameterKind.Output))
			{
				// The argument must be assignable.
				// The parameter type must be convertiable to the argument type.
				var boundArg = arg.Accept(this, null);
				var castedParameter = ImplicitCast(new VariableBoundExpression(arg, symbol), boundArg.Type);
				CheckAssignable(boundArg, MessageBag, arg.SourcePosition);
				return new(
					symbol,
					castedParameter,
					boundArg);
			}
			else if (symbol.Kind.Equals(ParameterKind.InOut))
			{
				// The argument must be assignable
				// The type must be identical
				var boundArg = arg.Accept(this, null);
				var boundParameter = new VariableBoundExpression(arg, symbol);
				if (!TypeRelations.IsIdentical(boundArg.Type, boundParameter.Type))
					MessageBag.Add(new InoutArgumentMustHaveSameTypeMessage(boundArg.Type, boundParameter.Type, arg.SourcePosition));
				CheckAssignable(boundArg, MessageBag, arg.SourcePosition);
				return new(
					symbol,
					boundParameter,
					boundArg);
			}
			else
			{
				throw new ArgumentException($"Unkown parameter kind '{symbol.Kind}'");
			}
		}

		public static void CheckAssignable(IBoundExpression expression, MessageBag messageBag, SourcePosition sourcePosition)
		{
			if (expression is not VariableBoundExpression)
				messageBag.Add(new CannotAssignToSyntaxMessage(sourcePosition));
		}
	}
}
