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
			else if (TypeRelations.IsAliasType(boundValue.Type, out var sourceAliasTypeSymbol))
			{
				var cast = new ImplicitAliasToBaseTypeCastBoundExpression(boundValue.OriginalNode, boundValue, sourceAliasTypeSymbol.AliasedType);
				return ImplicitCast(cast, targetType);
			}
			else if (TypeRelations.IsEnumType(boundValue.Type, out _))
			{
				var enumValue = new ImplicitEnumToBaseTypeCastBoundExpression(boundValue.OriginalNode, boundValue);
				return ImplicitCast(enumValue, targetType);
			}
			else if (TypeRelations.IsAliasType(targetType, out var aliasTypeSymbol))
			{
				var castBaseValue = ImplicitCast(boundValue, aliasTypeSymbol.AliasedType);
				return new ImplicitAliasFromBaseTypeCastBoundExpression(boundValue.OriginalNode, castBaseValue, aliasTypeSymbol);
			}
			else if (TypeRelations.IsPointerType(targetType, out var targetPointerType) && TypeRelations.IsPointerType(boundValue.Type, out _))
			{
				return new ImplicitPointerTypeCastBoundExpression(boundValue.OriginalNode, boundValue, targetPointerType);
			}
			else if (TypeRelations.IsBuiltInType(targetType, out var builtInTarget) && TypeRelations.IsBuiltInType(boundValue.Type, out var builtInSource) && SystemScope.IsAllowedArithmeticImplicitCast(builtInSource, builtInTarget))
			{
				return new ImplicitArithmeticCastBoundExpression(boundValue.OriginalNode, boundValue, targetType);
			}

			MessageBag.Add(new TypeIsNotConvertibleMessage(boundValue.Type, targetType, boundValue.OriginalNode.SourcePosition));
			return new ImplicitErrorCastBoundExpression(boundValue.OriginalNode, boundValue, targetType);
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
					commonArgType = ITypeSymbol.CreateError(binaryOperatorExpressionSyntax.TokenOperator.SourcePosition, default);
					operatorFunction = new OperatorFunction(FunctionSymbol.CreateError(binaryOperatorExpressionSyntax.TokenOperator.SourcePosition, returnType: commonArgType), false);
				}

				var returnType = operatorFunction.Value.Symbol.ReturnType ?? throw new InvalidOperationException("Invalid operator function, missing return value");
				var castedLeft = ImplicitCast(boundLeft, realCommonArgType);
				var castedRight = ImplicitCast(boundRight, realCommonArgType);
				IBoundExpression binaryOperatorExpression = new BinaryOperatorBoundExpression(binaryOperatorExpressionSyntax, returnType, castedLeft, castedRight, operatorFunction.Value.Symbol );
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
				return new PointerDiffrenceBoundExpression(binaryOperatorExpressionSyntax, castedLeft, castedRight, SystemScope.PointerDiffrence);
			}
			else if (TypeRelations.IsPointerType(baseLeftType, out var ptrLeft) && TypeRelations.IsBuiltInType(baseRightType, out var bright) && bright.IsInt && binaryOperatorExpressionSyntax.TokenOperator is MinusToken or PlusToken)
			{
				var castedLeft = ImplicitCast(boundLeft, baseLeftType);
				var castedRight = ImplicitCast(boundRight, SystemScope.PointerDiffrence);
				return ImplicitCast(new PointerOffsetBoundExpression(
					binaryOperatorExpressionSyntax,
					castedLeft,
					castedRight,
					ptrLeft), boundLeft.Type);

			}
			else if (TypeRelations.IsBuiltInType(baseLeftType, out var lright) && lright.IsInt && TypeRelations.IsPointerType(baseRightType, out var ptrRight) && binaryOperatorExpressionSyntax.TokenOperator is PlusToken)
			{
				var castedLeft = ImplicitCast(boundLeft, SystemScope.PointerDiffrence);
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
				operatorFunction = new OperatorFunction(FunctionSymbol.CreateError(unaryOperatorExpressionSyntax.TokenOperator.SourcePosition, returnType: boundValue.Type), false);
			}

			var returnType = operatorFunction.Value.Symbol.ReturnType ?? throw new InvalidOperationException("Invalid operator function, missing return value");
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
			return boundLeft.BindCompo(compoAccessExpressionSyntax, compoAccessExpressionSyntax.TokenIdentifier, context, this);
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

			var boundBase =  indexAccessExpressionSyntax.LeftSide.Accept(this, null);
			var castedIndices = indexAccessExpressionSyntax.Indices.Select(idx => ImplicitCast(idx.Accept(this, null), SystemScope.PointerDiffrence)).ToImmutableArray();
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
			throw new NotImplementedException();
		}
	}
}
