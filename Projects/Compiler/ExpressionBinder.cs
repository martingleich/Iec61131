using System;
using Compiler.Messages;

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
				=> ExpressionBinder.ImplicitCast(trueToken.SourcePosition, new LiteralBoundExpression(new BooleanLiteralValue(true)), context);

			public IBoundExpression Visit(FalseToken falseToken, IType? context)
				=> ExpressionBinder.ImplicitCast(falseToken.SourcePosition, new LiteralBoundExpression(new BooleanLiteralValue(false)), context);
			public IBoundExpression Visit(BooleanLiteralToken booleanLiteralToken, IType? context)
				=> ExpressionBinder.ImplicitCast(booleanLiteralToken.SourcePosition, new LiteralBoundExpression(new BooleanLiteralValue(booleanLiteralToken.Value)), context);

			public IBoundExpression Visit(TypedLiteralToken typedLiteralToken, IType? context)
			{
				var type = BuiltInTypeSymbol.MapTokenToType(typedLiteralToken.Value.Type);
				var boundValue = typedLiteralToken.Value.LiteralToken.Accept(this, type);
				return ExpressionBinder.ImplicitCast(typedLiteralToken.SourcePosition, boundValue, context);
			}

			public IBoundExpression Visit(IntegerLiteralToken integerLiteralToken, IType? context)
			{
				if (context != null)
				{
					if (TypeRelations.IsIdenticalType(context, BuiltInTypeSymbol.DInt))
					{
						ILiteralValue value;
						if (!integerLiteralToken.Value.TryGetInt(out int intValue))
						{
							value = new UnknownLiteralValue(context);
							MessageBag.Add(new ConstantDoesNotFitIntoType(integerLiteralToken, context));
						}
						else
						{
							value = new DIntLiteralValue(intValue);
						}
						return new LiteralBoundExpression(value);
					}
				}

				// Try int, uint, long, ulong
				if (integerLiteralToken.Value.TryGetInt(out int intValue2))
				{
					return ExpressionBinder.ImplicitCast(integerLiteralToken.SourcePosition, new LiteralBoundExpression(new DIntLiteralValue(intValue2)), context);
				}
				else
				{
					MessageBag.Add(new ConstantDoesNotFitIntoType(integerLiteralToken, BuiltInTypeSymbol.LInt));
					return new LiteralBoundExpression(new UnknownLiteralValue(context ?? BuiltInTypeSymbol.DInt));
				}
			}

			public IBoundExpression Visit(RealLiteralToken realLiteralToken, IType? context)
			{
				throw new NotImplementedException();
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

		private ExpressionBinder(IScope scope, MessageBag messageBag)
		{
			Scope = scope ?? throw new ArgumentNullException(nameof(scope));
			MessageBag = messageBag ?? throw new ArgumentNullException(nameof(messageBag));
			LiteralTokenBinder = new LiteralTokenBinderT(this);
		}

		public static IBoundExpression BindExpression(IScope scope, MessageBag messageBag, IExpressionSyntax syntax, IType? targetType)
			=> syntax.Accept(new ExpressionBinder(scope, messageBag), targetType);

		private IBoundExpression ImplicitCast(SourcePosition errorPosition, IBoundExpression boundValue, IType? targetType)
		{
			if (targetType == null || TypeRelations.IsIdenticalType(boundValue.Type, targetType))
			{
				return boundValue;
			}
			else if (Scope.CurrentEnum != null && TypeRelations.IsIdenticalType(boundValue.Type, Scope.CurrentEnum))
			{
				var enumValue = new ImplicitEnumToBaseTypeCastBoundExpression(boundValue);
				return ImplicitCast(errorPosition, enumValue, targetType);

			}
			else
			{
				MessageBag.Add(new TypeIsNotConvertibleMessage(boundValue.Type, targetType, errorPosition));
				return boundValue;
			}
		}

		public IBoundExpression Visit(LiteralExpressionSyntax literalExpressionSyntax, IType? context)
		{
			return literalExpressionSyntax.TokenValue.Accept(LiteralTokenBinder, context);
		}

		public IBoundExpression Visit(BinaryOperatorExpressionSyntax binaryOperatorExpressionSyntax, IType? context)
		{
			if (binaryOperatorExpressionSyntax.TokenOperator is PlusToken)
			{
				var boundLeft = binaryOperatorExpressionSyntax.Left.Accept(this, BuiltInTypeSymbol.DInt);
				var boundRight = binaryOperatorExpressionSyntax.Right.Accept(this, BuiltInTypeSymbol.DInt);
				return new AddBoundExpression(boundLeft.Type, boundLeft, boundRight);
			}
			throw new NotImplementedException();
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
			return ImplicitCast(sizeOfExpressionSyntax.SourcePosition, new SizeOfTypeBoundExpression(type), context);
		}

		public IBoundExpression Visit(CallExpressionSyntax callExpressionSyntax, IType? context)
		{
			throw new NotImplementedException();
		}
	}
}
