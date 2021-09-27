using System;
using Compiler.Messages;
using Compiler.Scopes;
using Compiler.Types;

namespace Compiler
{
	public sealed class ExpressionBinder : IExpressionSyntax.IVisitor<IBoundExpression, IType?>
	{
		private sealed class LiteralTokenBinderT : ILiteralToken.IVisitor<IBoundExpression, IType?>
		{
			private MessageBag MessageBag => ExpressionBinder.MessageBag;
			private readonly ExpressionBinder ExpressionBinder;

			public LiteralTokenBinderT(ExpressionBinder expressionBinder)
			{
				ExpressionBinder = expressionBinder ?? throw new ArgumentNullException(nameof(expressionBinder));
			}

			public IBoundExpression Visit(TrueToken trueToken, IType? context)
				=> ExpressionBinder.ImplicitCast(trueToken.SourcePosition, new LiteralBoundExpression(new BooleanLiteralValue(true, ExpressionBinder.SystemScope.Bool)), context);

			public IBoundExpression Visit(FalseToken falseToken, IType? context)
				=> ExpressionBinder.ImplicitCast(falseToken.SourcePosition, new LiteralBoundExpression(new BooleanLiteralValue(false, ExpressionBinder.SystemScope.Bool)), context);

			public IBoundExpression Visit(TypedLiteralToken typedLiteralToken, IType? context)
			{
				var type = ExpressionBinder.SystemScope.MapTokenToType(typedLiteralToken.Value.Type);
				var boundValue = typedLiteralToken.Value.LiteralToken.Accept(this, type);
				return ExpressionBinder.ImplicitCast(typedLiteralToken.SourcePosition, boundValue, context);
			}

			
			public IBoundExpression Visit(IntegerLiteralToken integerLiteralToken, IType? context)
			{
				ILiteralValue finalValue;
				if (context != null)
				{
					var value = ExpressionBinder.SystemScope.TryCreateLiteralFromIntValue(integerLiteralToken.Value, context);
					if(value == null)
					{
						MessageBag.Add(new IntegerIsToLargeForTypeMessage(integerLiteralToken.Value, context, integerLiteralToken.SourcePosition));
						value = new UnknownLiteralValue(context);
					}
					finalValue = value;
				}
				else
				{
					var value = ExpressionBinder.SystemScope.TryCreateIntLiteral(integerLiteralToken.Value);
					if (value == null)
					{
						MessageBag.Add(new ConstantDoesNotFitIntoAnyType(integerLiteralToken));
						value = new UnknownLiteralValue(ExpressionBinder.SystemScope.Int);
					}
					finalValue = value;
				}

				return new LiteralBoundExpression(finalValue);
			}

			public IBoundExpression Visit(RealLiteralToken realLiteralToken, IType? context)
			{
				if (context == null)
					context = ExpressionBinder.SystemScope.LReal;

				var value = ExpressionBinder.SystemScope.TryCreateLiteralFromRealValue(realLiteralToken.Value, context);
				if (value == null)
				{
					MessageBag.Add(new RealIsToLargeForTypeMessage(realLiteralToken.Value, context, realLiteralToken.SourcePosition));
					value = new UnknownLiteralValue(context);
				}
				return new LiteralBoundExpression(value);
			}

			public IBoundExpression Visit(SingleByteStringLiteralToken singleByteStringLiteralToken, IType? context)
			{
				throw new NotImplementedException();
			}

			public IBoundExpression Visit(MultiByteStringLiteralToken multiByteStringLiteralToken, IType? context)
			{
				throw new NotImplementedException();
			}

			public IBoundExpression Visit(DateLiteralToken dateLiteralToken, IType? context)
			{
				throw new NotImplementedException();
			}

			public IBoundExpression Visit(DurationLiteralToken durationLiteralToken, IType? context)
			{
				throw new NotImplementedException();
			}

			public IBoundExpression Visit(DateAndTimeLiteralToken dateAndTimeLiteralToken, IType? context)
			{
				throw new NotImplementedException();
			}
		}

		private readonly MessageBag MessageBag;
		private readonly LiteralTokenBinderT LiteralTokenBinder;
		private readonly IScope Scope;
		private SystemScope SystemScope => Scope.SystemScope;

		private ExpressionBinder(IScope scope, MessageBag messageBag)
		{
			Scope = scope ?? throw new ArgumentNullException(nameof(scope));
			MessageBag = messageBag ?? throw new ArgumentNullException(nameof(messageBag));
			LiteralTokenBinder = new LiteralTokenBinderT(this);
		}

		public static IBoundExpression Bind(IExpressionSyntax syntax, IScope scope, MessageBag messageBag, IType? targetType)
			=> syntax.Accept(new ExpressionBinder(scope, messageBag), targetType);

		private IBoundExpression ImplicitCast(SourcePosition errorPosition, IBoundExpression boundValue, IType? targetType)
		{
			if (targetType == null || TypeRelations.IsIdentical(boundValue.Type, targetType))
			{
				return boundValue;
			}
			else if (TypeRelations.IsEnumType(boundValue.Type, out _))
			{
				var enumValue = new ImplicitEnumToBaseTypeCastBoundExpression(boundValue);
				return ImplicitCast(errorPosition, enumValue, targetType);
			}
			else if (TypeRelations.IsPointerType(targetType, out var targetPointerType) && TypeRelations.IsPointerType(boundValue.Type, out _))
			{
				return new ImplicitPointerTypeCastBoundExpression(boundValue, targetPointerType);
			}
			else if (TypeRelations.IsBuiltInType(targetType, out var builtInTarget) && TypeRelations.IsBuiltInType(boundValue.Type, out var builtInSource) && SystemScope.IsAllowedArithmeticImplicitCast(builtInSource, builtInTarget))
			{
				return new ImplicitArithmeticCastBoundExpression(boundValue, targetType);
			}

			MessageBag.Add(new TypeIsNotConvertibleMessage(boundValue.Type, targetType, errorPosition));
			return boundValue;
		}

		public IBoundExpression Visit(LiteralExpressionSyntax literalExpressionSyntax, IType? context)
		{
			// "0" can be converted to every pointer type
			if (TypeRelations.IsPointerType(context, out var targetPointerType) && literalExpressionSyntax.TokenValue is IntegerLiteralToken intLiteral && intLiteral.Value.IsZero)
				return new LiteralBoundExpression(new NullPointerLiteralValue(targetPointerType));

			return literalExpressionSyntax.TokenValue.Accept(LiteralTokenBinder, context);
		}

		public IBoundExpression Visit(BinaryOperatorExpressionSyntax binaryOperatorExpressionSyntax, IType? context)
		{
			var boundLeft = binaryOperatorExpressionSyntax.Left.Accept(this, null);
			var boundRight = binaryOperatorExpressionSyntax.Right.Accept(this, null);
			// Special case pointer arithmetic:
			if (TypeRelations.IsPointerType(boundLeft.Type, out _) || TypeRelations.IsPointerType(boundRight.Type, out _))
			{
				var bound = VisitPointerArithmetic(binaryOperatorExpressionSyntax, boundLeft, boundRight, context);
				if (bound != null)
				{
					return ImplicitCast(binaryOperatorExpressionSyntax.SourcePosition, bound, context);
				}
				// Error-case, just to the normal flow to report errors.
			}

			// Perform naive overload resolution
			// This function only works if the arguments for the target function both have the same type.
			// i.e. ADD_DInt must take two DINTs, and so on.
			var commonArgType = SystemScope.GetSmallestCommonImplicitCastType(boundLeft.Type, boundRight.Type);
			FunctionSymbol? operatorFunction;
			if (TypeRelations.IsBuiltInType(commonArgType, out var b))
				operatorFunction = SystemScope.BuiltInFunctionTable.TryGetBinaryOperatorFunction(binaryOperatorExpressionSyntax.TokenOperator, b);
			else
				operatorFunction = null;

			if (operatorFunction == null)
			{
				MessageBag.Add(new CannotPerformArithmeticOnTypesMessage(binaryOperatorExpressionSyntax.TokenOperator.SourcePosition, boundLeft.Type, boundRight.Type));
				commonArgType = ITypeSymbol.CreateError(binaryOperatorExpressionSyntax.TokenOperator.SourcePosition, default);
				operatorFunction = FunctionSymbol.CreateError(binaryOperatorExpressionSyntax.TokenOperator.SourcePosition, returnType: commonArgType);
			}

			var returnType = operatorFunction.ReturnType ?? throw new InvalidOperationException("Invalid operator function, missing return value");
			var castedLeft = ImplicitCast(binaryOperatorExpressionSyntax.Left.SourcePosition, boundLeft, commonArgType);
			var castedRight = ImplicitCast(binaryOperatorExpressionSyntax.Right.SourcePosition, boundRight, commonArgType);
			var binaryOperatorExpression = new BinaryOperatorBoundExpression(returnType, castedLeft, castedRight, operatorFunction);
			return ImplicitCast(binaryOperatorExpressionSyntax.SourcePosition, binaryOperatorExpression, context);
		}

		private IBoundExpression? VisitPointerArithmetic(BinaryOperatorExpressionSyntax binaryOperatorExpressionSyntax, IBoundExpression boundLeft, IBoundExpression boundRight, IType? context)
		{
			if (TypeRelations.IsPointerType(boundLeft.Type, out _) && TypeRelations.IsPointerType(boundRight.Type, out _) && binaryOperatorExpressionSyntax.TokenOperator is MinusToken)
			{
				return new PointerDiffrenceBoundExpression(boundLeft, boundRight, SystemScope.PointerDiffrence);
			}
			else if (TypeRelations.IsPointerType(boundLeft.Type, out var ptrLeft) && TypeRelations.IsBuiltInType(boundRight.Type, out var bright) && bright.IsInt && binaryOperatorExpressionSyntax.TokenOperator is MinusToken or PlusToken)
			{
				var castedRight = ImplicitCast(binaryOperatorExpressionSyntax.TokenOperator.SourcePosition, boundRight, SystemScope.PointerDiffrence);
				return new PointerOffsetBoundExpression(
					boundLeft,
					castedRight,
					ptrLeft);

			}
			else if (TypeRelations.IsBuiltInType(boundLeft.Type, out var lright) && lright.IsInt && TypeRelations.IsPointerType(boundRight.Type, out var ptrRight) && binaryOperatorExpressionSyntax.TokenOperator is PlusToken)
			{
				var castedLeft = ImplicitCast(binaryOperatorExpressionSyntax.TokenOperator.SourcePosition, boundLeft, SystemScope.PointerDiffrence);
				return new PointerOffsetBoundExpression(
					castedLeft,
					boundLeft,
					ptrRight);
			}
			else
			{
				return null;
			}
		}

		public IBoundExpression Visit(UnaryOperatorExpressionSyntax unaryOperatorExpressionSyntax, IType? context)
		{
			var boundValue = unaryOperatorExpressionSyntax.Value.Accept(this, null);
			FunctionSymbol? operatorFunction;
			if (TypeRelations.IsBuiltInType(boundValue.Type, out var b))
				operatorFunction = SystemScope.BuiltInFunctionTable.TryGetUnaryOperatorFunction(unaryOperatorExpressionSyntax.TokenOperator, b);
			else
				operatorFunction = null;

			if (operatorFunction == null)
			{
				MessageBag.Add(new CannotPerformArithmeticOnTypesMessage(unaryOperatorExpressionSyntax.TokenOperator.SourcePosition, boundValue.Type));
				operatorFunction = FunctionSymbol.CreateError(unaryOperatorExpressionSyntax.TokenOperator.SourcePosition, returnType: boundValue.Type);
			}

			var returnType = operatorFunction.ReturnType ?? throw new InvalidOperationException("Invalid operator function, missing return value");
			var unaryOperatorExpression = new UnaryOperatorBoundExpression(returnType, boundValue, operatorFunction);
			return ImplicitCast(unaryOperatorExpressionSyntax.SourcePosition, unaryOperatorExpression, context);
		}

		public IBoundExpression Visit(ParenthesisedExpressionSyntax parenthesisedExpressionSyntax, IType? context)
			=> parenthesisedExpressionSyntax.Value.Accept(this, context);

		public IBoundExpression Visit(VariableExpressionSyntax variableExpressionSyntax, IType? context)
		{
			var variable = Scope.LookupVariable(variableExpressionSyntax.Identifier.ToCaseInsensitive(), variableExpressionSyntax.SourcePosition).Extract(MessageBag);
			var boundExpression = new VariableBoundExpression(variableExpressionSyntax, variable);
			return ImplicitCast(variableExpressionSyntax.SourcePosition, boundExpression, context);
		}

		public IBoundExpression Visit(CompoAccessExpressionSyntax compoAccessExpressionSyntax, IType? context)
		{
			throw new NotImplementedException();
		}

		public IBoundExpression Visit(DerefExpressionSyntax derefExpressionSyntax, IType? context)
		{
			var value = derefExpressionSyntax.LeftSide.Accept(this, null);
			IType baseType;
			if (TypeRelations.IsPointerType(value.Type, out var ptrType))
			{
				baseType = ptrType.BaseType;
			}
			else
			{
				MessageBag.Add(new CannotDereferenceTypeMessage(value.Type, derefExpressionSyntax.SourcePosition));
				baseType = value.Type;
			}

			var boundExpression = new DerefBoundExpression(value, baseType);
			return ImplicitCast(derefExpressionSyntax.SourcePosition, boundExpression, context);
		}

		public IBoundExpression Visit(IndexAccessExpressionSyntax indexAccessExpressionSyntax, IType? context)
		{
			throw new NotImplementedException();
		}

		public IBoundExpression Visit(SizeOfExpressionSyntax sizeOfExpressionSyntax, IType? context)
		{
			var type = TypeCompiler.MapSymbolic(Scope, sizeOfExpressionSyntax.Argument, MessageBag);
			return ImplicitCast(sizeOfExpressionSyntax.SourcePosition, new SizeOfTypeBoundExpression(type, Scope.SystemScope.Int), context);
		}

		public IBoundExpression Visit(CallExpressionSyntax callExpressionSyntax, IType? context)
		{
			throw new NotImplementedException();
		}
	}
}
