using System;
using System.Collections.Immutable;
using System.Linq;
using Compiler.Messages;
using Compiler.Scopes;
using Compiler.Types;

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
			else if (
				TypeRelations.IsBuiltInType(targetType, out var builtInTarget) && 
				TypeRelations.IsBuiltInType(boundValue.Type, out var builtInSource) &&
				SystemScope.BuiltInFunctionTable.TryGetCastFunction(builtInSource, builtInTarget) is FunctionVariableSymbol castFunction)
			{
				return new ImplicitCastBoundExpression(boundValue, castFunction);
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
					var function = FunctionVariableSymbol.CreateError(binaryOperatorExpressionSyntax.TokenOperator.SourcePosition,
						ImplicitName.ErrorBinaryOperator(boundLeft.Type.Code, boundRight.Type.Code, SystemScope.BuiltInFunctionTable.GetBinaryOperatorFunctionName(binaryOperatorExpressionSyntax.TokenOperator)));
					operatorFunction = new OperatorFunction(function, false);
				}

				var returnType = operatorFunction.Value.Symbol.Type.GetReturnType();
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
			// Since the parser does not parse integer tokens with the sign, we merge the negative sign into the integer token here.
			// This must be done in a special case here, because the normal typifier forbids negation for unsigned types and the default type for some literal values for example
			// (-int.Min) is unsigned, even if the -1 times this value would fit into the signed variant.
			if (unaryOperatorExpressionSyntax.Value is LiteralExpressionSyntax litExp && litExp.TokenValue is IntegerLiteralToken intLiteralToken)
			{
				if (unaryOperatorExpressionSyntax.TokenOperator is MinusToken)
				{
					var newValue = intLiteralToken.Value.GetNegative();
					return LiteralTokenBinder.BindIntLiteral(SystemScope, context, newValue, MessageBag, unaryOperatorExpressionSyntax);
				}
			}

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
				var function = FunctionVariableSymbol.CreateError(unaryOperatorExpressionSyntax.TokenOperator.SourcePosition,
					ImplicitName.ErrorUnaryOperator(boundValue.Type.Code, SystemScope.BuiltInFunctionTable.GetUnaryOperatorFunctionName(unaryOperatorExpressionSyntax.TokenOperator)));
				operatorFunction = new OperatorFunction(function, false);
			}

			var returnType = operatorFunction.Value.Symbol.Type.GetReturnType();
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
			var boundLeft = compoAccessExpressionSyntax.LeftSide.Accept(this, null);
			var name = compoAccessExpressionSyntax.TokenIdentifier;
			if (!(boundLeft.Type is StructuredTypeSymbol structuredType && structuredType.Fields.TryGetValue(name.Value, out var field)))
			{
				MessageBag.Add(!boundLeft.Type.IsError(), new FieldNotFoundMessage(boundLeft.Type, name.Value, name.SourcePosition));
				field = new FieldVariableSymbol(
					name.SourcePosition,
					name.Value,
					boundLeft.Type);
			}

			var boundCompo = new FieldAccessBoundExpression(compoAccessExpressionSyntax, boundLeft, field);
			return ImplicitCast(boundCompo, context);
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
				return ImplicitCast(new PointerIndexAccessBoundExpression(indexAccessExpressionSyntax, boundBase, pointerBaseType.BaseType, castedIndices), context);
			}
			else if (TypeRelations.IsArrayType(realBaseType, out var arrayBaseType))
			{
				CheckIndexCount(arrayBaseType.Ranges.Length, castedIndices);
				return ImplicitCast(new ArrayIndexAccessBoundExpression(indexAccessExpressionSyntax, boundBase, arrayBaseType.BaseType, castedIndices), context);
			}
			else
			{
				MessageBag.Add(new CannotIndexTypeMessage(boundBase.Type, indexAccessExpressionSyntax.TokenBracketOpen.SourcePosition));
				return ImplicitCast(new ArrayIndexAccessBoundExpression(indexAccessExpressionSyntax, boundBase, boundBase.Type, castedIndices), context);
			}
		}

		public IBoundExpression Visit(SizeOfExpressionSyntax sizeOfExpressionSyntax, IType? context)
		{
			var type = TypeCompiler.MapSymbolic(Scope, sizeOfExpressionSyntax.Argument, MessageBag);
			return ImplicitCast(new SizeOfTypeBoundExpression(sizeOfExpressionSyntax, type, Scope.SystemScope.Int), context);
		}

		public IBoundExpression Visit(CallExpressionSyntax callExpressionSyntax, IType? context)
		{
			var boundCallee = callExpressionSyntax.Callee.Accept(this, null);
			if (boundCallee.Type is not ICallableTypeSymbol callableType)
			{
				MessageBag.Add(!boundCallee.Type.IsError(), new CannotCallTypeMessage(boundCallee.Type, callExpressionSyntax.Callee.SourcePosition));
				callableType = FunctionTypeSymbol.CreateError(callExpressionSyntax.Callee.SourcePosition, boundCallee.Type);
			}
			var boundArgs = BindFunctionCall(callableType, callExpressionSyntax.Arguments);
			var boundCall = new CallBoundExpression(callExpressionSyntax, boundCallee, boundArgs, callableType.GetReturnType());
			return ImplicitCast(boundCall, context);

			ImmutableArray<BoundCallArgument> BindFunctionCall(
				ICallableTypeSymbol function,
				SyntaxCommaSeparated<CallArgumentSyntax> args)
			{
				bool isError = function.IsError();
				// Bind arguments
				if (args.Count != function.GetParameterCountWithoutReturn())
				{
					// Error wrong number of arguments.
					MessageBag.Add(!isError, new WrongNumberOfArgumentsMessage(function, args.Count, args.SourcePosition));
				}

				var boundArguments = ImmutableArray.CreateBuilder<BoundCallArgument>();
				int nextParamId = 0;
				bool afterExplicit = false;
				foreach (var arg in args)
				{
					ParameterVariableSymbol symbol;
					//	Find assigned argument
					if (arg.ExplicitParameter is ExplicitCallParameterSyntax explicitParameterSyntax)
					{
						if (!function.Parameters.TryGetValue(explicitParameterSyntax.Identifier, out var explicitParameter) ||
							explicitParameterSyntax.Identifier == function.Name) // The implicit output for return is not accessable from the outside
						{
							MessageBag.Add(!isError, new ParameterNotFoundMessage(function, explicitParameterSyntax.Identifier, explicitParameterSyntax.TokenIdentifier.SourcePosition));
							symbol = ParameterVariableSymbol.CreateError(explicitParameterSyntax.Identifier, explicitParameterSyntax.TokenIdentifier.SourcePosition);
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
							symbol = ParameterVariableSymbol.CreateError(nextParamId, arg.SourcePosition);
						}
						else if (nextParamId >= function.Parameters.Length)
						{
							symbol = ParameterVariableSymbol.CreateError(nextParamId, arg.SourcePosition);
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
				return boundArgumentsFrozen;

				void CheckForDuplicateParameter(ImmutableArray<BoundCallArgument> boundArguments)
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

				BoundCallArgument BindCallArgument(ParameterVariableSymbol symbol, IExpressionSyntax arg)
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
						IsLValueChecker.IsLValue(boundArg).Extract(MessageBag);
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
						IsLValueChecker.IsLValue(boundArg).Extract(MessageBag);
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
			}
		}

		public IBoundExpression Visit(ScopedVariableExpressionSyntax scopedVariableExpressionSyntax, IType? context)
		{
			var scope = Scope.ResolveScope(scopedVariableExpressionSyntax.Scope).Extract(MessageBag, out bool missingScope);
			IVariableSymbol variable;
			if (missingScope)
				variable = IVariableSymbol.CreateError(scopedVariableExpressionSyntax.TokenIdentifier.SourcePosition, scopedVariableExpressionSyntax.Identifier);
			else
				variable = scope.LookupVariable(scopedVariableExpressionSyntax.Identifier, scopedVariableExpressionSyntax.TokenIdentifier.SourcePosition).Extract(MessageBag);
			IBoundExpression boundExpression = new VariableBoundExpression(scopedVariableExpressionSyntax, variable);
			if (scope is AliasTypeSymbol aliasType && variable is EnumVariableSymbol)
			{
				// Accessing an enum value via an alias, shall yield an alias value.
				boundExpression = ImplicitCast(boundExpression, aliasType);
			}
			return ImplicitCast(boundExpression, context);
		}

	}
}
