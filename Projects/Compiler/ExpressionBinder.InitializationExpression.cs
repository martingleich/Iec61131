using Compiler.Messages;
using Compiler.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Compiler
{
	public sealed partial class ExpressionBinder
	{
		public IBoundExpression Visit(InitializationExpressionSyntax initializationExpressionSyntax, IType? context)
		{
			IType targetType;
			if (context != null)
			{
				targetType = context;
			}
			else
			{
				MessageBag.Add(new CannotInferTypeForInitializerMessage(initializationExpressionSyntax.SourcePosition));
				targetType = ITypeSymbol.CreateError(initializationExpressionSyntax.SourcePosition, ImplicitName.ErrorType);
			}

			var resolvedTargetType = TypeRelations.ResolveAlias(targetType);
			IBoundExpression baseBound;
			if (resolvedTargetType is StructuredTypeSymbol structuredType && !structuredType.IsUnion)
			{
				if (structuredType.IsUnion)
					throw new NotImplementedException("Initializer for UNION");
				else
					baseBound = StructuredTypeInitializer.Bind(structuredType, structuredType.Fields, this, initializationExpressionSyntax);
			}
			else if (resolvedTargetType is FunctionBlockSymbol fbSymbol)
			{
				baseBound = StructuredTypeInitializer.Bind(fbSymbol, fbSymbol.Fields, this, initializationExpressionSyntax);
			}
			else if (resolvedTargetType is ArrayType arrayType)
			{
				baseBound = ArrayInitializer.Bind(arrayType, this, initializationExpressionSyntax);
			}
			else
			{
				MessageBag.Add(new CannotUseAnInitializerForThisTypeMessage(initializationExpressionSyntax.SourcePosition));
				baseBound = new InitializerBoundExpression(ImmutableArray<InitializerBoundExpression.ABoundElement>.Empty, targetType, initializationExpressionSyntax);
			}
			return ImplicitCast(baseBound, context);
		}

		private static class ArrayInitializer
		{
			public static IBoundExpression Bind(ArrayType type, ExpressionBinder binder, InitializationExpressionSyntax syntax)
			{
				if (type.Ranges.Length == 1)
					return OneDimensionalArrayInitializer.Bind(type, binder, syntax);
				else
					return MultiDimensionalArrayInitializer.Bind(type, binder, syntax);
			}
			class OneDimensionalArrayInitializer : IInitializerElementSyntax.IVisitor
			{
				private readonly ArrayType.Range _range;
				private readonly IType _elementType;
				private MessageBag Messages => _expressionBinder.MessageBag;
				private readonly ExpressionBinder _expressionBinder;

				private int _implicitCursor;
				private bool _inPositionalPart;
				private bool _reportedImplicitError;

				private readonly Dictionary<int, SourcePosition> _setIndicies = new();
				private readonly ImmutableArray<InitializerBoundExpression.ABoundElement>.Builder _elements = ImmutableArray.CreateBuilder<InitializerBoundExpression.ABoundElement>();

				private OneDimensionalArrayInitializer( ArrayType.Range range, IType elementType, ExpressionBinder binder)
				{
					_elementType = elementType ?? throw new ArgumentNullException(nameof(elementType));
					_range = range;
					_implicitCursor = _range.LowerBound;
					_expressionBinder = binder ?? throw new ArgumentNullException(nameof(binder));
				}

				void IInitializerElementSyntax.IVisitor.Visit(FieldInitializerElementSyntax fieldInitializerElementSyntax)
				{
					Messages.Add(new TypeDoesNotHaveThisElementMessage(fieldInitializerElementSyntax.TokenName.SourcePosition));
				}
				void IInitializerElementSyntax.IVisitor.Visit(IndexInitializerElementSyntax indexInitializerElementSyntax)
				{
					_inPositionalPart = true;
					var boundIndex = indexInitializerElementSyntax.Index.Accept(_expressionBinder, _expressionBinder.SystemScope.DInt);
					var indexValue = (DIntLiteralValue?)ConstantExpressionEvaluator.EvaluateConstant(_expressionBinder.SystemScope, boundIndex, Messages);
					if (indexValue != null)
					{
						var index = new BoundConstantIntegerValue(boundIndex, indexValue.Value);
						AddElement(indexInitializerElementSyntax.Index.SourcePosition, index, indexInitializerElementSyntax.Value);
					}
				}
				void IInitializerElementSyntax.IVisitor.Visit(ExpressionElementSyntax expressionElementSyntax)
				{
					if (_inPositionalPart)
					{
						if (!_reportedImplicitError)
						{
							Messages.Add(new CannotUsePositionalElementAfterExplicitMessage(expressionElementSyntax.SourcePosition));
							_reportedImplicitError = true;
						}
					}
					else
					{
						++_implicitCursor;
						var index = new BoundConstantIntegerValue(null, _implicitCursor);
						AddElement(expressionElementSyntax.SourcePosition, index, expressionElementSyntax.Value);
					}
				}
				void IInitializerElementSyntax.IVisitor.Visit(AllIndicesInitializerElementSyntax allIndicesInitializerElementSyntax)
				{
					var boundValue = allIndicesInitializerElementSyntax.Value.Accept(_expressionBinder, _elementType);
					bool isValid = true;
					foreach (var i in _range.Values)
						isValid &= MarkIndexUsed(i, allIndicesInitializerElementSyntax.TokenDots.SourcePosition);

					if (isValid)
					{
						var element = new InitializerBoundExpression.ABoundElement.AllElements(boundValue);
						_elements.Add(element);
					}
				}

				private bool MarkIndexUsed(int index, SourcePosition indexPosition)
				{
					if (!_setIndicies.TryAdd(index, indexPosition))
					{
						var original = _setIndicies[index];
						Messages.Add(new DuplicateInitializerElementMessage(indexPosition, original));
						return false;
					}
					else
					{
						return true;
					}
				}
				private void AddElement(SourcePosition indexPosition, BoundConstantIntegerValue index, IExpressionSyntax value)
				{
					if (!_range.IsInRange(index.Value))
					{
						Messages.Add(new TypeDoesNotHaveThisElementMessage(indexPosition));
						return;
					}

					var boundValue = value.Accept(_expressionBinder, _elementType);
					if (MarkIndexUsed(index.Value, indexPosition))
					{
						var element = new InitializerBoundExpression.ABoundElement.ArrayElement(ImmutableArray.Create(index), boundValue);
						_elements.Add(element);
					}
				}

				private InitializerBoundExpression GetBound(INode originalNode, IType type)
				{
					foreach (var i in _range.Values)
					{
						if (!_setIndicies.ContainsKey(i))
							Messages.Add(new IndexNotInitializedMessage(i, originalNode.SourcePosition));
					}
					return new InitializerBoundExpression(_elements.ToImmutable(), type, originalNode);
				}

				public static InitializerBoundExpression Bind(ArrayType type, ExpressionBinder binder, InitializationExpressionSyntax syntax)
				{
					var initializer = new OneDimensionalArrayInitializer(type.Ranges[0], type.BaseType, binder);
					foreach (var element in syntax.Elements)
						element.Accept(initializer);
					return initializer.GetBound(syntax, type);
				}
			}
			class MultiDimensionalArrayInitializer : IInitializerElementSyntax.IVisitor
			{
				private readonly IType _elementType;
				private MessageBag Messages => _expressionBinder.MessageBag;
				private readonly ExpressionBinder _expressionBinder;
				private readonly ImmutableArray<InitializerBoundExpression.ABoundElement>.Builder _elements = ImmutableArray.CreateBuilder<InitializerBoundExpression.ABoundElement>();
				private SourcePosition _originalBound;

				private MultiDimensionalArrayInitializer(IType elementType, ExpressionBinder expressionBinder)
				{
					_elementType = elementType ?? throw new ArgumentNullException(nameof(elementType));
					_expressionBinder = expressionBinder ?? throw new ArgumentNullException(nameof(expressionBinder));
				}

				public void Visit(FieldInitializerElementSyntax fieldInitializerElementSyntax)
				{
					Messages.Add(new TypeDoesNotHaveThisElementMessage(fieldInitializerElementSyntax.TokenName.SourcePosition));
				}

				public void Visit(IndexInitializerElementSyntax indexInitializerElementSyntax)
				{
					Messages.Add(new TypeDoesNotHaveThisElementMessage(indexInitializerElementSyntax.Index.SourcePosition));
				}

				public void Visit(AllIndicesInitializerElementSyntax allIndicesInitializerElementSyntax)
				{
					if (_elements.Count != 0)
						Messages.Add(new DuplicateInitializerElementMessage(allIndicesInitializerElementSyntax.TokenDots.SourcePosition, _originalBound));

					var boundValue = allIndicesInitializerElementSyntax.Value.Accept(_expressionBinder, _elementType);
					_elements.Add(new InitializerBoundExpression.ABoundElement.AllElements(boundValue));
					_originalBound = allIndicesInitializerElementSyntax.TokenDots.SourcePosition;
				}

				public void Visit(ExpressionElementSyntax expressionElementSyntax)
				{
					Messages.Add(new CannotUseImplicitInitializerForThisTypeMessage(expressionElementSyntax.SourcePosition));
				}
				private InitializerBoundExpression GetBound(INode originalNode, IType type)
				{
					if (_elements.Count == 0)
						Messages.Add(new MissingElementsInInitializerMessage(originalNode.SourcePosition));
					return new InitializerBoundExpression(_elements.ToImmutable(), type, originalNode);
				}

				public static InitializerBoundExpression Bind(ArrayType type, ExpressionBinder binder, InitializationExpressionSyntax syntax)
				{
					var initializer = new MultiDimensionalArrayInitializer(type.BaseType, binder);
					foreach (var element in syntax.Elements)
						element.Accept(initializer);
					return initializer.GetBound(syntax, type);
				}
			}
		}

		private class StructuredTypeInitializer : IInitializerElementSyntax.IVisitor
		{
			private readonly IType _type;
			private readonly SymbolSet<FieldVariableSymbol> _fields;
			private readonly ExpressionBinder _binder;
			private MessageBag Messages => _binder.MessageBag;
			private readonly Dictionary<FieldVariableSymbol, SourcePosition> _setFields = new(SymbolByNameComparer<FieldVariableSymbol>.Instance);
			private readonly ImmutableArray<InitializerBoundExpression.ABoundElement>.Builder _elements = ImmutableArray.CreateBuilder<InitializerBoundExpression.ABoundElement>();

			private StructuredTypeInitializer(IType type, SymbolSet<FieldVariableSymbol> fields, ExpressionBinder binder)
			{
				_type = type ?? throw new ArgumentNullException(nameof(type));
				_fields = fields;
				_binder = binder ?? throw new ArgumentNullException(nameof(binder));
			}

			private void MarkUsed(FieldVariableSymbol field, SourcePosition position)
			{
				if (!_setFields.TryAdd(field, position))
				{
					var original = _setFields[field];
					Messages.Add(new DuplicateInitializerElementMessage(position, original));
				}
			}
			void IInitializerElementSyntax.IVisitor.Visit(FieldInitializerElementSyntax fieldInitializerElementSyntax)
			{
				InitializerBoundExpression.ABoundElement element;
				if (_fields.TryGetValue(fieldInitializerElementSyntax.Name, out var field))
				{
					MarkUsed(field, fieldInitializerElementSyntax.TokenName.SourcePosition);
					var boundValue = fieldInitializerElementSyntax.Value.Accept(_binder, field.Type);
					element = new InitializerBoundExpression.ABoundElement.FieldElement(field, boundValue);
				}
				else
				{
					Messages.Add(new FieldNotFoundMessage(_type, fieldInitializerElementSyntax.Name, fieldInitializerElementSyntax.TokenName.SourcePosition));
					var boundValue = fieldInitializerElementSyntax.Value.Accept(_binder, null);
					element = new InitializerBoundExpression.ABoundElement.UnknownElement(boundValue);
				}
				_elements.Add(element);
			}
			void IInitializerElementSyntax.IVisitor.Visit(IndexInitializerElementSyntax indexInitializerElementSyntax)
			{
				Messages.Add(new TypeDoesNotHaveThisElementMessage(indexInitializerElementSyntax.Index.SourcePosition));
			}
			void IInitializerElementSyntax.IVisitor.Visit(AllIndicesInitializerElementSyntax allIndicesInitializerElementSyntax)
			{
				Messages.Add(new TypeDoesNotHaveThisElementMessage(allIndicesInitializerElementSyntax.TokenDots.SourcePosition));
			}
			void IInitializerElementSyntax.IVisitor.Visit(ExpressionElementSyntax expressionElementSyntax)
			{
				Messages.Add(new CannotUseImplicitInitializerForThisTypeMessage(expressionElementSyntax.SourcePosition));
			}

			private IBoundExpression Bind(INode originalNode)
			{
				foreach (var field in _fields)
					if (!_setFields.ContainsKey(field))
						Messages.Add(new FieldNotInitializedMessage(field, originalNode.SourcePosition));

				return new InitializerBoundExpression(_elements.ToImmutable(), _type, originalNode);
			}

			public static IBoundExpression Bind(IType type, SymbolSet<FieldVariableSymbol> fields, ExpressionBinder binder, InitializationExpressionSyntax syntax)
			{
				var initializer = new StructuredTypeInitializer(type, fields, binder);
				foreach (var element in syntax.Elements)
					element.Accept(initializer);
				return initializer.Bind(syntax);
			}
		}
	}
}
