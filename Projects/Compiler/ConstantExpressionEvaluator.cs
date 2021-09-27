﻿using Compiler.Messages;
using Compiler.Scopes;
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

		public static ILiteralValue? EvaluateConstant(IScope scope, MessageBag messageBag, IType type, IExpressionSyntax expression)
		{
			var boundExpr = ExpressionBinder.Bind(expression, scope, messageBag, type);
			return EvaluateConstant(boundExpr, messageBag, scope.SystemScope);
		}
		public static ILiteralValue? EvaluateConstant(IBoundExpression expression, MessageBag messages, SystemScope systemScope)
			=> expression.Accept(new ConstantExpressionEvaluator(messages, systemScope));

		public ILiteralValue? Accept(BinaryOperatorBoundExpression binaryOperatorBoundExpression)
		{
			var leftValue = binaryOperatorBoundExpression.Left.Accept(this);
			var rightValue = binaryOperatorBoundExpression.Right.Accept(this);
			return EvaluateConstantFunction(binaryOperatorBoundExpression.Type, binaryOperatorBoundExpression.Function, leftValue, rightValue);
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
				return NotAConstant(default);
			}
		}

		public ILiteralValue? Accept(UnaryOperatorBoundExpression unaryOperatorBoundExpression)
		{
			var value = unaryOperatorBoundExpression.Value.Accept(this);
			return EvaluateConstantFunction(unaryOperatorBoundExpression.Type, unaryOperatorBoundExpression.Function, value);
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
				return NotAConstant(variableBoundExpression.OriginalSyntax?.SourcePosition);
		}

		public ILiteralValue? Visit(ImplicitEnumToBaseTypeCastBoundExpression implicitEnumCastBoundExpression)
		{
			var x = implicitEnumCastBoundExpression.Value.Accept(this);
			return (x as EnumLiteralValue)?.InnerValue; // No error necessary, typify already generates one.
		}

		public ILiteralValue? Visit(ImplicitPointerTypeCastBoundExpression implicitPointerTypeCaseBoundExpression) => NotAConstant(default);
		public ILiteralValue? Visit(PointerDiffrenceBoundExpression pointerDiffrenceBoundExpression) => NotAConstant(default);
		public ILiteralValue? Accept(PointerOffsetBoundExpression pointerOffsetBoundExpression) => NotAConstant(default);

		private ILiteralValue? NotAConstant(SourcePosition? position)
		{
			MessageBag.Add(new NotAConstantMessage(position ?? default));
			return null;
		}

		private ILiteralValue? EvaluateConstantFunction(IType returnType, FunctionSymbol function, params ILiteralValue?[] args)
		{
			if (!args.HasNoNullElement(out var nonNullArgs))
				return null; // The args are not constant, this is already an error. Do not report an error again.

			if (!SystemScope.BuiltInFunctionTable.TryGetConstantEvaluator(function, out var func))
				return NotAConstant(default);

			try
			{
				return func(returnType, nonNullArgs);
			}
			catch (InvalidCastException) // The values have the wrong type, i.e. The expression binder must already reported an error for this
			{
				return null;
			}
			catch (DivideByZeroException) // Divsion by zero in constant context
			{
				MessageBag.Add(new DivsionByZeroInConstantContextMessage(default));
				return null;
			}
			catch (OverflowException) // Overflow in constant context
			{
				MessageBag.Add(new OverflowInConstantContextMessage(default));
				return null;
			}
		}
	}
}
