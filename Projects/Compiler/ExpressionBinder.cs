using System;
using Compiler.Messages;
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
					if (TypeRelations.IsIdentical(context, ExpressionBinder.SystemScope.Bool))
					{
						if (integerLiteralToken.Value.IsZero)
							finalValue = new BooleanLiteralValue(false, context);
						else if (integerLiteralToken.Value.IsOne)
							finalValue = new BooleanLiteralValue(true, context);
						else
						{
							MessageBag.Add(new ConstantDoesNotFitIntoType(integerLiteralToken, context));
							finalValue = new BooleanLiteralValue(false, context);
						}
					}
					else if (TypeRelations.IsIdentical(context, ExpressionBinder.SystemScope.Real))
					{
						if (integerLiteralToken.Value.TryGetSingle(out var x))
						{
							finalValue = new RealLiteralValue(x, context);
						}
						else
						{
							MessageBag.Add(new ConstantDoesNotFitIntoType(integerLiteralToken, context));
							finalValue = new RealLiteralValue(0, context);
						}
					}
					else if (TypeRelations.IsIdentical(context, ExpressionBinder.SystemScope.LReal))
					{
						if (integerLiteralToken.Value.TryGetDouble(out var x))
						{
							finalValue = new LRealLiteralValue(x, context);
						}
						else
						{
							MessageBag.Add(new ConstantDoesNotFitIntoType(integerLiteralToken, context));
							finalValue = new LRealLiteralValue(0, context);
						}
					}
					else
					{
						var value = ExpressionBinder.SystemScope.TryCreateIntLiteral(integerLiteralToken.Value, context);
						if (value == null)
						{
							MessageBag.Add(new ConstantDoesNotFitIntoType(integerLiteralToken, context));
							value = new UnknownLiteralValue(context);
						}
						finalValue = value;
					}
				}
				else
				{
					var value = ExpressionBinder.SystemScope.TryCreateIntLiteral(integerLiteralToken.Value);
					if (value == null)
					{
						MessageBag.Add(new ConstantDoesNotFitIntoAnyType(integerLiteralToken));
						value = new UnknownLiteralValue(ExpressionBinder.SystemScope.DInt);
					}
					finalValue = value;
				}

				return new LiteralBoundExpression(finalValue);
			}

			public IBoundExpression Visit(RealLiteralToken realLiteralToken, IType? context)
			{
				if (context == null || TypeRelations.IsIdentical(context, ExpressionBinder.SystemScope.LReal))
				{
					if (!realLiteralToken.Value.TryGetDouble(out var x))
					{
						MessageBag.Add(new ConstantDoesNotFitIntoType(realLiteralToken, ExpressionBinder.SystemScope.LReal));
						x = 0;
					}
					return new LiteralBoundExpression(new LRealLiteralValue(x, ExpressionBinder.SystemScope.LReal));
				}
				else
				{
					if (TypeRelations.IsIdentical(context, ExpressionBinder.SystemScope.Real))
					{
						if (!realLiteralToken.Value.TryGetSingle(out var x))
						{
							MessageBag.Add(new ConstantDoesNotFitIntoType(realLiteralToken, ExpressionBinder.SystemScope.Real));
							x = 0;
						}
						return new LiteralBoundExpression(new RealLiteralValue(x, ExpressionBinder.SystemScope.Real));
					}
					else
					{
						MessageBag.Add(new ConstantDoesNotFitIntoType(realLiteralToken, ExpressionBinder.SystemScope.Real));
						return new LiteralBoundExpression(new UnknownLiteralValue(context));
					}
				}
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
			else if (Scope.CurrentEnum != null && TypeRelations.IsIdentical(boundValue.Type, Scope.CurrentEnum))
			{
				var enumValue = new ImplicitEnumToBaseTypeCastBoundExpression(boundValue);
				return ImplicitCast(errorPosition, enumValue, targetType);
			}
			else if (targetType is PointerType targetPointerType && boundValue.Type is PointerType)
			{
				return new ImplicitPointerTypeCastBoundExpression(boundValue, targetPointerType);
			}
			else if (SystemScope.IsIntegerType(targetType) && boundValue is LiteralBoundExpression literalBoundExpression && literalBoundExpression.Value is IAnyIntLiteralValue anyIntValue)
			{
				var resultValue = SystemScope.TryCreateIntLiteral(anyIntValue.Value, targetType);
				if (resultValue == null)
				{
					MessageBag.Add(new ConstantValueIsToLargeForTargetMessage(anyIntValue.Value, targetType, errorPosition));
					return new LiteralBoundExpression(new UnknownLiteralValue(targetType));
				}
				else
				{
					return new LiteralBoundExpression(resultValue);
				}
			}
			else if (TypeRelations.IsIdentical(targetType, SystemScope.Real) && boundValue is LiteralBoundExpression literalBoundExpression2 && literalBoundExpression2.Value is IAnyIntLiteralValue anyIntValue2)
			{
				if (anyIntValue2.Value.TryGetSingle(out float value))
				{
					return new LiteralBoundExpression(new RealLiteralValue(value, targetType));
				}
				else
				{
					MessageBag.Add(new ConstantValueIsToLargeForTargetMessage(anyIntValue2.Value, targetType, errorPosition));
					return new LiteralBoundExpression(new UnknownLiteralValue(targetType));
				}
			}
			else if (TypeRelations.IsIdentical(targetType, SystemScope.LReal) && boundValue is LiteralBoundExpression literalBoundExpression3 && literalBoundExpression3.Value is IAnyIntLiteralValue anyIntValue3)
			{
				if (anyIntValue3.Value.TryGetDouble(out double value))
				{
					return new LiteralBoundExpression(new LRealLiteralValue(value, targetType));
				}
				else
				{
					MessageBag.Add(new ConstantValueIsToLargeForTargetMessage(anyIntValue3.Value, targetType, errorPosition));
					return new LiteralBoundExpression(new UnknownLiteralValue(targetType));
				}
			}
			else if (TypeRelations.IsIdentical(targetType, SystemScope.LReal) && boundValue is LiteralBoundExpression literalBoundExpression4 && literalBoundExpression4.Value is RealLiteralValue realLiteralValue)
			{
				return new LiteralBoundExpression(new LRealLiteralValue(realLiteralValue.Value, targetType));
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
			if (binaryOperatorExpressionSyntax.TokenOperator.Accept(MapBinaryToArithmeticName.Instance) is string opName)
				return BindBinaryArithmetic(binaryOperatorExpressionSyntax, context, opName);
			throw new NotImplementedException();
		}

		private IBoundExpression BindBinaryArithmetic(BinaryOperatorExpressionSyntax binaryOperatorExpressionSyntax, IType? context, string opName)
		{
			var boundLeft = binaryOperatorExpressionSyntax.Left.Accept(this, null);
			var boundRight = binaryOperatorExpressionSyntax.Right.Accept(this, null);
			var maxArithmeticType = GetPromotedArithmeticType(boundLeft.Type, boundRight.Type);
			FunctionSymbol operatorFunction;
			if (maxArithmeticType is BuiltInType b)
			{
				operatorFunction = SystemScope.GetOperatorFunction(opName, b);
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

		private sealed class MapBinaryToArithmeticName : IBinaryOperatorToken.IVisitor<string?>
		{
			public static readonly MapBinaryToArithmeticName Instance = new();
			public string? Visit(EqualToken equalToken) => null;
			public string? Visit(LessEqualToken lessEqualToken) => null;
			public string? Visit(LessToken lessToken) => null;
			public string? Visit(GreaterToken greaterToken) => null;
			public string? Visit(GreaterEqualToken greaterEqualToken) => null;
			public string? Visit(UnEqualToken unEqualToken) => null;
			public string? Visit(PlusToken plusToken) => "ADD";
			public string? Visit(MinusToken minusToken) => "SUB";
			public string? Visit(StarToken starToken) => "MUL";
			public string? Visit(SlashToken slashToken) => "DIV";
			public string? Visit(PowerToken powerToken) => null;
			public string? Visit(AndToken andToken) => null;
			public string? Visit(XorToken xorToken) => null;
			public string? Visit(OrToken orToken) => null;
			public string? Visit(ModToken modToken) => null;
		}

		private IType? GetPromotedArithmeticType(IType a, IType b)
		{
			// Enum + Anyting => BaseType(Enum) + Anything
			// LREAL + Anything => LREAL
			// REAL + Anything => REAL
			// Signed + Signed => The bigger one
			// Unsigned + Unsigned => The bigger one
			// Signed + Unsigned => The next signed type that is bigger than both.
			if (a is EnumTypeSymbol enumA)
				return GetPromotedArithmeticType(enumA.BaseType, b);
			if (b is EnumTypeSymbol enumB)
				return GetPromotedArithmeticType(a, enumB.BaseType);
			if (a is BuiltInType builtInA && b is BuiltInType builtInB && builtInA.IsArithmetic && builtInB.IsArithmetic)
			{
				if (builtInA.Equals(SystemScope.LReal) || builtInB.Equals(SystemScope.LReal))
					return SystemScope.LReal;
				if (builtInA.Equals(SystemScope.Real) || builtInB.Equals(SystemScope.Real))
					return SystemScope.Real;
				if (builtInA.IsUnsigned == builtInB.IsUnsigned)
					return builtInB.Size > builtInA.Size ? builtInB : builtInA;
				if (SystemScope.Int.Size > builtInA.Size && SystemScope.Int.Size > builtInB.Size)
					return SystemScope.Int;
				if (SystemScope.DInt.Size > builtInA.Size && SystemScope.DInt.Size > builtInB.Size)
					return SystemScope.DInt;
				if (SystemScope.LInt.Size > builtInA.Size && SystemScope.LInt.Size > builtInB.Size)
					return SystemScope.LInt;
			}
			return null;
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
			return ImplicitCast(sizeOfExpressionSyntax.SourcePosition, new SizeOfTypeBoundExpression(type, Scope.SystemScope.DInt), context);
		}

		public IBoundExpression Visit(CallExpressionSyntax callExpressionSyntax, IType? context)
		{
			throw new NotImplementedException();
		}
	}
}
