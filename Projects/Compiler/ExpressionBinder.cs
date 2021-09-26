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
			else if (boundValue.Type is EnumTypeSymbol)
			{
				var enumValue = new ImplicitEnumToBaseTypeCastBoundExpression(boundValue);
				return ImplicitCast(errorPosition, enumValue, targetType);
			}
			else if (targetType is PointerType targetPointerType && boundValue.Type is PointerType)
			{
				return new ImplicitPointerTypeCastBoundExpression(boundValue, targetPointerType);
			}
			else if (targetType is BuiltInType builtInTarget && boundValue.Type is BuiltInType builtInSource && SystemScope.IsAllowedArithmeticImplicitCast(builtInSource, builtInTarget))
			{
				return new ImplicitArithmeticCastBoundExpression(boundValue, targetType);
			}

			MessageBag.Add(new TypeIsNotConvertibleMessage(boundValue.Type, targetType, errorPosition));
			return boundValue;
		}

		public IBoundExpression Visit(LiteralExpressionSyntax literalExpressionSyntax, IType? context)
		{
			// "0" can be converted to every pointer type
			if (context is PointerType targetPointerType && literalExpressionSyntax.TokenValue is IntegerLiteralToken intLiteral && intLiteral.Value.IsZero)
				return new LiteralBoundExpression(new NullPointerLiteralValue(targetPointerType));

			return literalExpressionSyntax.TokenValue.Accept(LiteralTokenBinder, context);
		}

		public IBoundExpression Visit(BinaryOperatorExpressionSyntax binaryOperatorExpressionSyntax, IType? context)
		{
			if (SystemScope.BuiltInFunctionTable.MapBinaryOperatorToOpId(binaryOperatorExpressionSyntax.TokenOperator) is BuiltInFunctionTable.BuiltInId opId)
				return BindBinaryArithmetic(binaryOperatorExpressionSyntax, context, opId);
			throw new NotImplementedException();
		}

		private IBoundExpression BindBinaryArithmetic(BinaryOperatorExpressionSyntax binaryOperatorExpressionSyntax, IType? context, BuiltInFunctionTable.BuiltInId opName)
		{
			var boundLeft = binaryOperatorExpressionSyntax.Left.Accept(this, null);
			var boundRight = binaryOperatorExpressionSyntax.Right.Accept(this, null);
			var maxArithmeticType = SystemScope.GetSmallestCommonImplicitCastType(boundLeft.Type, boundRight.Type);
			FunctionSymbol operatorFunction;
			if (maxArithmeticType is BuiltInType b)
			{
				operatorFunction = SystemScope.BuiltInFunctionTable.GetOperatorFunction(opName, b);
			}
			else
			{
				MessageBag.Add(new CannotPerformArithmeticOnTypesMessage(binaryOperatorExpressionSyntax.TokenOperator.SourcePosition, boundLeft.Type, boundRight.Type));
				maxArithmeticType = ITypeSymbol.CreateError(binaryOperatorExpressionSyntax.TokenOperator.SourcePosition, default);
				operatorFunction = FunctionSymbol.CreateError(binaryOperatorExpressionSyntax.TokenOperator.SourcePosition);
			}

			var castedLeft = ImplicitCast(binaryOperatorExpressionSyntax.Left.SourcePosition, boundLeft, maxArithmeticType);
			var castedRight = ImplicitCast(binaryOperatorExpressionSyntax.Right.SourcePosition, boundRight, maxArithmeticType);
			return ImplicitCast(
				binaryOperatorExpressionSyntax.SourcePosition,
				new BinaryOperatorBoundExpression(maxArithmeticType, castedLeft, castedRight, operatorFunction),
				context);
		}


		public IBoundExpression Visit(UnaryOperatorExpressionSyntax unaryOperatorExpressionSyntax, IType? context)
		{
			throw new NotImplementedException();
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
			throw new NotImplementedException();
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
