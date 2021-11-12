using Compiler.Messages;
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
			return EvaluateConstant(scope.SystemScope, boundExpr, messageBag);
		}
		public static ILiteralValue? EvaluateConstant(SystemScope systemScope, IBoundExpression expression, MessageBag messages)
			=> expression.Accept(new ConstantExpressionEvaluator(messages, systemScope));

		private ILiteralValue? NotAConstant(IBoundExpression node) => NotAConstant(node.OriginalNode);
		private ILiteralValue? NotAConstant(INode node)
		{
			MessageBag.Add(new NotAConstantMessage(node.SourcePosition));
			return null;
		}
		private ILiteralValue? EvaluateConstantFunction(IBoundExpression expression, FunctionSymbol function, params ILiteralValue?[] args)
		{
			if (!args.HasNoNullElement(out var nonNullArgs))
				return null; // The args are not constant, this is already an error. Do not report an error again.

			if (!SystemScope.BuiltInFunctionTable.TryGetConstantEvaluator(function, out var func))
				return NotAConstant(expression);

			try
			{
				return func(expression.Type, nonNullArgs);
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
		
		public ILiteralValue? Accept(BinaryOperatorBoundExpression binaryOperatorBoundExpression)
		{
			var leftValue = binaryOperatorBoundExpression.Left.Accept(this);
			var rightValue = binaryOperatorBoundExpression.Right.Accept(this);
			return EvaluateConstantFunction(binaryOperatorBoundExpression, binaryOperatorBoundExpression.Function, leftValue, rightValue);
		}

		public ILiteralValue? Visit(ImplicitArithmeticCastBoundExpression implicitArithmeticCastBoundExpression)
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
				return NotAConstant(implicitArithmeticCastBoundExpression.OriginalNode);
			}
		}

		public ILiteralValue? Visit(UnaryOperatorBoundExpression unaryOperatorBoundExpression)
		{
			var value = unaryOperatorBoundExpression.Value.Accept(this);
			return EvaluateConstantFunction(unaryOperatorBoundExpression, unaryOperatorBoundExpression.Function, value);
		}

		public ILiteralValue? Visit(LiteralBoundExpression literalBoundExpression) => literalBoundExpression.Value;
		public ILiteralValue? Visit(SizeOfTypeBoundExpression sizeOfTypeBoundExpression)
		{
			var undefinedLayoutInf = DelayedLayoutType.GetLayoutInfo(sizeOfTypeBoundExpression.ArgType, MessageBag, default);
			if (undefinedLayoutInf.TryGet(out var layoutInfo))
				return new IntLiteralValue(checked((short)layoutInfo.Size), sizeOfTypeBoundExpression.Type);
			else
				return null;
		}
		public ILiteralValue? Visit(VariableBoundExpression variableBoundExpression)
		{
			if (variableBoundExpression.Variable is EnumVariableSymbol enumValueSymbol)
				return enumValueSymbol._GetConstantValue(MessageBag);
			else
				return NotAConstant(variableBoundExpression.OriginalNode);
		}

		public ILiteralValue? Visit(ImplicitEnumToBaseTypeCastBoundExpression implicitEnumCastBoundExpression)
		{
			var x = implicitEnumCastBoundExpression.Value.Accept(this);
			return (x as EnumLiteralValue)?.InnerValue; // No error necessary, typify already generates one.
		}

		public ILiteralValue? Visit(ImplicitPointerTypeCastBoundExpression implicitPointerTypeCaseBoundExpression) => NotAConstant(implicitPointerTypeCaseBoundExpression);
		public ILiteralValue? Visit(PointerDiffrenceBoundExpression pointerDiffrenceBoundExpression) => NotAConstant(pointerDiffrenceBoundExpression);
		public ILiteralValue? Visit(PointerOffsetBoundExpression pointerOffsetBoundExpression) => NotAConstant(pointerOffsetBoundExpression);
		public ILiteralValue? Visit(DerefBoundExpression derefBoundExpression) => NotAConstant(derefBoundExpression);
		public ILiteralValue? Visit(ImplicitAliasToBaseTypeCastBoundExpression aliasToBaseTypeCastBoundExpression) => NotAConstant(aliasToBaseTypeCastBoundExpression);

		public ILiteralValue? Visit(ImplicitErrorCastBoundExpression implicitErrorCastBoundExpression)
		{
			// This is never a constant, since it is only generated for compile errors, report error for the inner values, and go on.
			implicitErrorCastBoundExpression.Value.Accept(this);
			return null;
		}

		public ILiteralValue? Visit(ImplicitAliasFromBaseTypeCastBoundExpression implicitAliasFromBaseTypeCastBoundExpression) => NotAConstant(implicitAliasFromBaseTypeCastBoundExpression);
		public ILiteralValue? Visit(ArrayIndexAccessBoundExpression arrayIndexAccessBoundExpression) => NotAConstant(arrayIndexAccessBoundExpression);
		public ILiteralValue? Visit(PointerIndexAccessBoundExpression pointerIndexAccessBoundExpression) => NotAConstant(pointerIndexAccessBoundExpression);
		public ILiteralValue? Visit(FieldAccessBoundExpression fieldAccessBoundExpression) => NotAConstant(fieldAccessBoundExpression);
		public ILiteralValue? Visit(StaticVariableBoundExpression staticVariableBoundExpression) => NotAConstant(staticVariableBoundExpression);
		public ILiteralValue? Visit(FunctionCallBoundExpression functionCallBoundExpression) => NotAConstant(functionCallBoundExpression);
		public ILiteralValue? Visit(ImplicitDiscardBoundExpression implicitDiscardBoundExpression) => NotAConstant(implicitDiscardBoundExpression);
		public ILiteralValue? Visit(FunctionBlockCallBoundExpression functionBlockCallBoundExpression) => NotAConstant(functionBlockCallBoundExpression);
	}
}
