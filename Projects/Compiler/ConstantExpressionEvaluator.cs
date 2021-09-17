using Compiler.Messages;
using Compiler.Types;
using System;

namespace Compiler
{
	public sealed class ConstantExpressionEvaluator : IBoundExpression.IVisitor<ILiteralValue?>
	{
		private readonly MessageBag Messages;

		public ConstantExpressionEvaluator(MessageBag messages)
		{
			Messages = messages ?? throw new ArgumentNullException(nameof(messages));
		}

		public static ILiteralValue? EvaluateConstant(IBoundExpression expression, MessageBag messages)
			=> expression.Accept(new ConstantExpressionEvaluator(messages));
		public static ILiteralValue? EvaluateConstant(IScope scope, MessageBag messageBag, IType type, IExpressionSyntax expression)
		{
			var boundExpr = ExpressionBinder.BindExpression(scope, messageBag, expression, type);
			return EvaluateConstant(boundExpr, messageBag);
		}

		public ILiteralValue? Accept(AddBoundExpression addBoundExpression)
		{
			var leftValue = addBoundExpression.Left.Accept(this);
			var rightValue = addBoundExpression.Right.Accept(this);
			if (leftValue is DIntLiteralValue leftLiteral && rightValue is DIntLiteralValue rightLiteral)
				return new DIntLiteralValue(leftLiteral.Value + rightLiteral.Value);
			else
				return null;
		}

		public ILiteralValue? Accept(BinaryOperatorBoundExpression binaryOperatorBoundExpression)
		{
			var leftValue = binaryOperatorBoundExpression.Left.Accept(this);
			var rightValue = binaryOperatorBoundExpression.Right.Accept(this);
			if (leftValue == null || rightValue == null)
				return null;
			switch (binaryOperatorBoundExpression.Function.Name.Original.ToUpperInvariant())
			{
				case "ADD_DINT": return new DIntLiteralValue(((DIntLiteralValue)leftValue).Value + ((DIntLiteralValue)rightValue).Value);
				case "SUB_DINT": return new DIntLiteralValue(((DIntLiteralValue)leftValue).Value - ((DIntLiteralValue)rightValue).Value);
				case "MUL_DINT": return new DIntLiteralValue(((DIntLiteralValue)leftValue).Value * ((DIntLiteralValue)rightValue).Value);
				case "DIV_DINT": return new DIntLiteralValue(((DIntLiteralValue)leftValue).Value / ((DIntLiteralValue)rightValue).Value);
				default: return null;
			}
		}

		public ILiteralValue? Visit(LiteralBoundExpression literalBoundExpression) => literalBoundExpression.Value;
		public ILiteralValue? Visit(SizeOfTypeBoundExpression sizeOfTypeBoundExpression) => new DIntLiteralValue(
			DelayedLayoutType.GetLayoutInfo(sizeOfTypeBoundExpression.Type, Messages, default).Size);
		public ILiteralValue? Visit(VariableBoundExpression variableBoundExpression)
		{
			if (variableBoundExpression.Variable is EnumValueSymbol enumValueSymbol)
				return enumValueSymbol._GetConstantValue(Messages);
			else
			{
				Messages.Add(new NotAConstantMessage(variableBoundExpression.OriginalSyntax?.SourcePosition ?? default));
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
	}
}
