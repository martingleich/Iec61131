using Compiler.Messages;

namespace Compiler
{
	public sealed class IsLValueChecker : IBoundExpression.IVisitor<ErrorsAnd<bool>>
	{
		private readonly static IsLValueChecker Instance = new();
		public static ErrorsAnd<bool> IsLValue(IBoundExpression expression) => expression.Accept(Instance);
		private static ErrorsAnd<bool> NotAssignable(IBoundExpression expression) => ErrorsAnd.Create(false, new CannotAssignToSyntaxMessage(expression.OriginalNode.SourcePosition));

		public ErrorsAnd<bool> Visit(VariableBoundExpression variableBoundExpression)
		{
			if (variableBoundExpression.Variable is FunctionVariableSymbol funcVar)
				return ErrorsAnd.Create(false, new CannotAssignToVariableMessage(funcVar, variableBoundExpression.OriginalNode.SourcePosition));
			else
				return ErrorsAnd.Create(true);
		}
		public ErrorsAnd<bool> Visit(DerefBoundExpression derefBoundExpression) => ErrorsAnd.Create(true);
		public ErrorsAnd<bool> Visit(ArrayIndexAccessBoundExpression arrayIndexAccessBoundExpression) => IsLValue(arrayIndexAccessBoundExpression.Base);
		public ErrorsAnd<bool> Visit(PointerIndexAccessBoundExpression pointerIndexAccessBoundExpression) => ErrorsAnd.Create(true);
		public ErrorsAnd<bool> Visit(FieldAccessBoundExpression fieldAccessBoundExpression) => IsLValue(fieldAccessBoundExpression.BaseExpression);


		public ErrorsAnd<bool> Visit(BinaryOperatorBoundExpression binaryOperatorBoundExpression) => NotAssignable(binaryOperatorBoundExpression);
		public ErrorsAnd<bool> Visit(LiteralBoundExpression literalBoundExpression) => NotAssignable(literalBoundExpression);
		public ErrorsAnd<bool> Visit(SizeOfTypeBoundExpression sizeOfTypeBoundExpression) => NotAssignable(sizeOfTypeBoundExpression);
		public ErrorsAnd<bool> Visit(ImplicitEnumToBaseTypeCastBoundExpression implicitEnumCastBoundExpression) => NotAssignable(implicitEnumCastBoundExpression);
		public ErrorsAnd<bool> Visit(ImplicitPointerTypeCastBoundExpression implicitPointerTypeCaseBoundExpression) => NotAssignable(implicitPointerTypeCaseBoundExpression);
		public ErrorsAnd<bool> Visit(ImplicitArithmeticCastBoundExpression implicitArithmeticCaseBoundExpression) => NotAssignable(implicitArithmeticCaseBoundExpression);
		public ErrorsAnd<bool> Visit(UnaryOperatorBoundExpression unaryOperatorBoundExpression) => NotAssignable(unaryOperatorBoundExpression);
		public ErrorsAnd<bool> Visit(PointerDiffrenceBoundExpression pointerDiffrenceBoundExpression) => NotAssignable(pointerDiffrenceBoundExpression);
		public ErrorsAnd<bool> Visit(PointerOffsetBoundExpression pointerOffsetBoundExpression) => NotAssignable(pointerOffsetBoundExpression);
		public ErrorsAnd<bool> Visit(ImplicitAliasToBaseTypeCastBoundExpression aliasToBaseTypeCastBoundExpression) => NotAssignable(aliasToBaseTypeCastBoundExpression);
		public ErrorsAnd<bool> Visit(ImplicitErrorCastBoundExpression implicitErrorCastBoundExpression) => NotAssignable(implicitErrorCastBoundExpression);
		public ErrorsAnd<bool> Visit(ImplicitAliasFromBaseTypeCastBoundExpression implicitAliasFromBaseTypeCastBoundExpression) => NotAssignable(implicitAliasFromBaseTypeCastBoundExpression);
		public ErrorsAnd<bool> Visit(StaticVariableBoundExpression staticVariableBoundExpression) => NotAssignable(staticVariableBoundExpression);
		public ErrorsAnd<bool> Visit(CallBoundExpression functionCallBoundExpression) => NotAssignable(functionCallBoundExpression);
		public ErrorsAnd<bool> Visit(ImplicitDiscardBoundExpression implicitDiscardBoundExpression) => NotAssignable(implicitDiscardBoundExpression);
	}
}
