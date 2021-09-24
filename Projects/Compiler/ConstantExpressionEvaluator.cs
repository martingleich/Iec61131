using Compiler.Messages;
using Compiler.Types;
using System;

namespace Compiler
{
	public sealed class ConstantExpressionEvaluator : IBoundExpression.IVisitor<ILiteralValue?>
	{
		private readonly MessageBag MessageBag;
		private readonly SystemScope SystemScope;

		public ConstantExpressionEvaluator(MessageBag messages, SystemScope systemScope)
		{
			MessageBag = messages ?? throw new ArgumentNullException(nameof(messages));
			SystemScope = systemScope ?? throw new ArgumentNullException(nameof(systemScope));
		}

		public static ILiteralValue? EvaluateConstant(IBoundExpression expression, MessageBag messages, SystemScope systemScope)
			=> expression.Accept(new ConstantExpressionEvaluator(messages, systemScope));
		public static ILiteralValue? EvaluateConstant(IScope scope, MessageBag messageBag, IType type, IExpressionSyntax expression)
		{
			var boundExpr = ExpressionBinder.Bind(expression, scope, messageBag, type);
			return EvaluateConstant(boundExpr, messageBag, scope.SystemScope);
		}

		public ILiteralValue? Accept(BinaryOperatorBoundExpression binaryOperatorBoundExpression)
		{
			var leftValue = binaryOperatorBoundExpression.Left.Accept(this);
			var rightValue = binaryOperatorBoundExpression.Right.Accept(this);
			if (leftValue == null || rightValue == null)
				return null;
			try
			{
				switch (binaryOperatorBoundExpression.Function.Name.Original.ToUpperInvariant())
				{
					case "ADD_DINT": return new DIntLiteralValue(((DIntLiteralValue)leftValue).Value + ((DIntLiteralValue)rightValue).Value, binaryOperatorBoundExpression.Type);
					case "SUB_DINT": return new DIntLiteralValue(((DIntLiteralValue)leftValue).Value - ((DIntLiteralValue)rightValue).Value, binaryOperatorBoundExpression.Type);
					case "MUL_DINT": return new DIntLiteralValue(((DIntLiteralValue)leftValue).Value * ((DIntLiteralValue)rightValue).Value, binaryOperatorBoundExpression.Type);
					case "DIV_DINT": return new DIntLiteralValue(((DIntLiteralValue)leftValue).Value / ((DIntLiteralValue)rightValue).Value, binaryOperatorBoundExpression.Type);
					default: return null;
				}
			}
			catch (InvalidCastException) // The values have the wrong type, i.e. The expression binder must already reported an error for this
			{
				return null;
			}
		}

		public ILiteralValue? Accept(ImplicitArithmeticCastBoundExpression implicitArithmeticCastBoundExpression)
		{
			var value = implicitArithmeticCastBoundExpression.Value.Accept(this);
			if (value == null)
				return null;

			IType targetType = implicitArithmeticCastBoundExpression.Type;
			if (value is IAnyIntLiteralValue intLiteralValue)
			{
				// Integer to Integer|Real|LReal
				var resultValue = SystemScope.TryCreateLiteralFromIntValue(intLiteralValue.Value, targetType);
				if (resultValue == null)
				{
					MessageBag.Add(new ConstantValueIsToLargeForTargetMessage(intLiteralValue.Value, targetType, default));
					return new UnknownLiteralValue(targetType);
				}
				else
				{
					return resultValue;
				}
			}
			else if (TypeRelations.IsIdentical(targetType, SystemScope.LReal) && value is RealLiteralValue realLiteralValue)
			{
				return new LRealLiteralValue(realLiteralValue.Value, targetType);
			}
			else
			{
				MessageBag.Add(new NotAConstantMessage(default));
				return null;
			}
		}

		public ILiteralValue? Visit(LiteralBoundExpression literalBoundExpression) => literalBoundExpression.Value;
		public ILiteralValue? Visit(SizeOfTypeBoundExpression sizeOfTypeBoundExpression)
		{
			var undefinedLayoutInf = DelayedLayoutType.GetLayoutInfo(sizeOfTypeBoundExpression.ArgType, MessageBag, default);
			if (undefinedLayoutInf.TryGet(out var layoutInfo))
				return new DIntLiteralValue(layoutInfo.Size, sizeOfTypeBoundExpression.Type);
			else
				return null;
		}
		public ILiteralValue? Visit(VariableBoundExpression variableBoundExpression)
		{
			if (variableBoundExpression.Variable is EnumValueSymbol enumValueSymbol)
				return enumValueSymbol._GetConstantValue(MessageBag);
			else
			{
				MessageBag.Add(new NotAConstantMessage(variableBoundExpression.OriginalSyntax?.SourcePosition ?? default));
				return null;
			}
		}

		public ILiteralValue? Visit(ImplicitEnumToBaseTypeCastBoundExpression implicitEnumCastBoundExpression)
		{
			var x = implicitEnumCastBoundExpression.Value.Accept(this);
			return x is EnumLiteralValue enumLiteralValue
				? enumLiteralValue.InnerValue
				: null; // No error necessary, typify already generates one.
		}

		public ILiteralValue? Visit(ImplicitPointerTypeCastBoundExpression implicitPointerTypeCaseBoundExpression)
		{
			MessageBag.Add(new NotAConstantMessage(default));
			return null;
		}
	}
}
