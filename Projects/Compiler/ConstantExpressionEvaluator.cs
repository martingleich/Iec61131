using Compiler.Messages;
using Compiler.Scopes;
using Compiler.Types;
using StandardLibraryExtensions;
using System;
using System.Collections.Immutable;
using System.Linq;

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

        private ILiteralValue? NotAConstant(IBoundExpression node)
        {
            MessageBag.Add(new NotAConstantMessage(node.GetSourcePositionOrDefault()));
            return null;
        }
        private ILiteralValue? EvaluateConstantFunction(IBoundExpression expression, FunctionVariableSymbol function, params ILiteralValue?[] args)
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
                MessageBag.Add(new DivsionByZeroInConstantContextMessage(expression.GetSourcePositionOrDefault()));
                return null;
            }
            catch (OverflowException) // Overflow in constant context
            {
                MessageBag.Add(new OverflowInConstantContextMessage(expression.GetSourcePositionOrDefault()));
                return null;
            }
        }

        public ILiteralValue? Visit(BinaryOperatorBoundExpression binaryOperatorBoundExpression)
        {
            var leftValue = binaryOperatorBoundExpression.Left.Accept(this);
            var rightValue = binaryOperatorBoundExpression.Right.Accept(this);
            return EvaluateConstantFunction(binaryOperatorBoundExpression, binaryOperatorBoundExpression.Function, leftValue, rightValue);
        }

        public ILiteralValue? Visit(ImplicitCastBoundExpression implicitArithmeticCastBoundExpression)
        {
            var value = implicitArithmeticCastBoundExpression.Value.Accept(this);
            return EvaluateConstantFunction(implicitArithmeticCastBoundExpression, implicitArithmeticCastBoundExpression.CastFunction, value);
        }

        public ILiteralValue? Visit(UnaryOperatorBoundExpression unaryOperatorBoundExpression)
        {
            var value = unaryOperatorBoundExpression.Value.Accept(this);
            return EvaluateConstantFunction(unaryOperatorBoundExpression, unaryOperatorBoundExpression.Function, value);
        }

        public ILiteralValue? Visit(LiteralBoundExpression literalBoundExpression) => literalBoundExpression.Value;
        public ILiteralValue? Visit(SizeOfTypeBoundExpression sizeOfTypeBoundExpression)
        {
            var undefinedLayoutInf = DelayedLayoutType.GetLayoutInfo(sizeOfTypeBoundExpression.ArgType, MessageBag, sizeOfTypeBoundExpression.GetSourcePositionOrDefault());
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
                return NotAConstant(variableBoundExpression);
        }

        public ILiteralValue? Visit(ImplicitEnumToBaseTypeCastBoundExpression implicitEnumCastBoundExpression)
        {
            var x = implicitEnumCastBoundExpression.Value.Accept(this) as EnumLiteralValue;
            return x?.InnerValue; // No error necessary, typify already generates one.
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
        public ILiteralValue? Visit(PointerIndexAccessBoundExpression pointerIndexAccessBoundExpression) => NotAConstant(pointerIndexAccessBoundExpression);
        public ILiteralValue? Visit(CallBoundExpression functionCallBoundExpression) => NotAConstant(functionCallBoundExpression);
        public ILiteralValue? Visit(ImplicitDiscardBoundExpression implicitDiscardBoundExpression) => NotAConstant(implicitDiscardBoundExpression);
        public ILiteralValue? Visit(InitializerBoundExpression initializerBoundExpression)
        {
            return initializerBoundExpression.Accept(
            arrayInitializer => {
                var arrayType = arrayInitializer.Type;
                var elements = new ILiteralValue[arrayType.ElementCount];
                foreach (var element in arrayInitializer.Elements)
                {
                    var value = element.Value.Accept(this);
                    if (value == null)
                        return null;
                    switch (element)
                    {
                        case InitializerBoundExpression.ABoundElement.ArrayElement arrayElement:
                            elements[arrayElement.Index.Value] = value;
                            break;
                        case InitializerBoundExpression.ABoundElement.AllElements:
                            return new ArrayLiteralValue.AllSameArrayLiteralValue(arrayType, value);
                        default:
                            return NotAConstant(arrayInitializer);
                    }
                }
                return new ArrayLiteralValue.SimpleArrayLiteralValue(arrayType, elements.ToImmutableArray());
            },
            structInitializer => {
                var dict = ImmutableDictionary.CreateBuilder<CaseInsensitiveString, ILiteralValue>();
                foreach (var element in structInitializer.Elements)
                {
                    var value = element.Value.Accept(this);
                    if (value == null)
                        return null;
                    switch (element)
                    {
                        case InitializerBoundExpression.ABoundElement.FieldElement fieldElement:
                            dict[fieldElement.Field.Name] = value;
                            break;
                        default:
                            return NotAConstant(structInitializer);
                    }
                }
                return new StructuredLiteralValue(dict.ToImmutable(), structInitializer.Type);
            },
            unknownInitializer => NotAConstant(unknownInitializer));
        }
        public ILiteralValue? Visit(ArrayIndexAccessBoundExpression arrayIndexAccessBoundExpression)
        {
            var arrayBase = arrayIndexAccessBoundExpression.Base.Accept(this) as ArrayLiteralValue;
            if (arrayBase == null)
                return null;
            if (arrayBase.Type.Ranges.Length != arrayIndexAccessBoundExpression.Indices.Length)
                return null; // Can happen with non-working code.
            var literalIndices = arrayIndexAccessBoundExpression.Indices.Select(idx => idx.Accept(this) as DIntLiteralValue).ToArray();
            if (!literalIndices.HasNoNullElement(out var nonNullLiteralIndices))
                return null;
            var indices = nonNullLiteralIndices.Select(v => v.Value).ToImmutableArray();
            var maybeIndex = arrayBase.Type.GetIndexOf(indices);
            if (maybeIndex is not int index)
            {
                MessageBag.Add(new OutOfBoundsAccessInConstantContextMessage(
                    arrayIndexAccessBoundExpression.GetSourcePositionOrDefault(),
                    indices,
                    arrayBase.Type.Ranges));
                return null;
            }

            return arrayBase.GetElement(index);
        }
        public ILiteralValue? Visit(FieldAccessBoundExpression fieldAccessBoundExpression)
        {
            var @base = fieldAccessBoundExpression.BaseExpression.Accept(this) as StructuredLiteralValue;
            return @base?.GetElement(fieldAccessBoundExpression.Field);
        }
    }
}
