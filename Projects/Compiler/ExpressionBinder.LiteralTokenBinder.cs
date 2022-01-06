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
				var type = ExpressionBinder.SystemScope.BuiltInTypeTable.MapTokenToType(typedLiteralToken.Value.Type);
				var boundValue = typedLiteralToken.Value.LiteralToken.Accept(this, type);
				return ExpressionBinder.ImplicitCast(boundValue, context);
			}

			public IBoundExpression Visit(IntegerLiteralToken integerLiteralToken, IType? context)
				=> BindIntLiteral(ExpressionBinder.SystemScope, context, integerLiteralToken.Value, MessageBag, integerLiteralToken);

			public static IBoundExpression BindIntLiteral(SystemScope systemScope, IType? context, OverflowingInteger value, MessageBag messageBag, INode originalNode)
			{
				ILiteralValue? literalValue;
				if (context != null)
					literalValue = systemScope.TryCreateLiteralFromIntValue(value, context);
				else
					literalValue = systemScope.TryCreateIntLiteral(value);

				if (literalValue == null)
				{
					messageBag.Add(new ConstantDoesNotFitIntoTypeMessage(SyntaxToStringConverter.ExactToString(originalNode), context, originalNode.SourceSpan));
					literalValue = new UnknownLiteralValue(context ?? systemScope.Int);
				}

				return new LiteralBoundExpression(originalNode, literalValue);
			}

			public IBoundExpression Visit(RealLiteralToken realLiteralToken, IType? context)
			{
				if (context == null)
					context = ExpressionBinder.SystemScope.LReal;

				var value = ExpressionBinder.SystemScope.TryCreateLiteralFromRealValue(realLiteralToken.Value, context);
				if (value == null)
				{
					MessageBag.Add(new ConstantDoesNotFitIntoTypeMessage(realLiteralToken.Generating ?? realLiteralToken.Value.ToString(), context, realLiteralToken.SourceSpan));
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
				if (context == null)
					context = ExpressionBinder.SystemScope.Time;

				var value = ExpressionBinder.SystemScope.TryCreateLiteralFromDurationValue(durationLiteralToken.Value, context);
				if (value == null)
				{
					MessageBag.Add(new ConstantDoesNotFitIntoTypeMessage(durationLiteralToken.Generating ?? durationLiteralToken.Value.ToString(), context, durationLiteralToken.SourceSpan));
					value = new UnknownLiteralValue(context);
				}
				return new LiteralBoundExpression(durationLiteralToken, value);
			}

			public IBoundExpression Visit(DateAndTimeLiteralToken dateAndTimeLiteralToken, IType? context)
			{
				throw new NotImplementedException();
			}
		}
	}
}
