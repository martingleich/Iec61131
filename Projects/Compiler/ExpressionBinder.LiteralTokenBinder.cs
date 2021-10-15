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

				return new LiteralBoundExpression(integerLiteralToken, finalValue);
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
	}
}
