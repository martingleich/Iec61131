using System;
using Compiler.Messages;
using Compiler.Types;

namespace Compiler
{
	public sealed partial class ExpressionBinder
	{
		private sealed class LiteralTokenBinder : ILiteralToken.IVisitor<IBoundExpression, IType?>
		{
			private MessageBag MessageBag => ExpressionBinder.MessageBag;
			private readonly ExpressionBinder ExpressionBinder;

			public LiteralTokenBinder(ExpressionBinder expressionBinder)
			{
				ExpressionBinder = expressionBinder ?? throw new ArgumentNullException(nameof(expressionBinder));
			}

			public IBoundExpression Visit(TrueToken trueToken, IType? context)
				=> ExpressionBinder.ImplicitCast(new LiteralBoundExpression(trueToken, new BooleanLiteralValue(true, ExpressionBinder.SystemScope.Bool)), context);

			public IBoundExpression Visit(FalseToken falseToken, IType? context)
				=> ExpressionBinder.ImplicitCast(new LiteralBoundExpression(falseToken, new BooleanLiteralValue(false, ExpressionBinder.SystemScope.Bool)), context);

			public IBoundExpression Visit(TypedLiteralToken typedLiteralToken, IType? context)
			{
				var type = ExpressionBinder.SystemScope.MapTokenToType(typedLiteralToken.Value.Type);
				var boundValue = typedLiteralToken.Value.LiteralToken.Accept(this, type);
				return ExpressionBinder.ImplicitCast(boundValue, context);
			}


			public IBoundExpression Visit(IntegerLiteralToken integerLiteralToken, IType? context)
				=> BindIntLiteral(ExpressionBinder.SystemScope, context, integerLiteralToken.Value, MessageBag, integerLiteralToken);

			public static IBoundExpression BindIntLiteral(SystemScope systemScope, IType? context, OverflowingInteger value, MessageBag messageBag, INode originalNode)
			{
				ILiteralValue finalLiteralValue;
				if (context != null)
				{
					var literalValue = systemScope.TryCreateLiteralFromIntValue(value, context);
					if(literalValue == null)
					{
						messageBag.Add(new IntegerIsToLargeForTypeMessage(value, context, originalNode.SourcePosition));
						literalValue = new UnknownLiteralValue(context);
					}
					finalLiteralValue = literalValue;
				}
				else
				{
					var literalValue = systemScope.TryCreateIntLiteral(value);
					if (literalValue == null)
					{
						messageBag.Add(new ConstantDoesNotFitIntoAnyType(SyntaxToStringConverter.ExactToString(originalNode), originalNode.SourcePosition));
						literalValue = new UnknownLiteralValue(systemScope.Int);
					}
					finalLiteralValue = literalValue;
				}

				return new LiteralBoundExpression(originalNode, finalLiteralValue);
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
				return new LiteralBoundExpression(realLiteralToken, value);
			}

			public IBoundExpression Visit(StringLiteralToken stringLiteralToken, IType? context)
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
	}
}
