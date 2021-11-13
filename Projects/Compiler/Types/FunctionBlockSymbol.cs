using Compiler.Messages;
using System;

namespace Compiler.Types
{
	public sealed class FunctionBlockSymbol : ICallableTypeSymbol, ITypeSymbol, _IDelayedLayoutType
	{
		public string Code => Name.Original;
		public SourcePosition DeclaringPosition { get; }
		public CaseInsensitiveString Name { get; }

		private readonly StructuredLayoutHelper _layoutHelper;
		public SymbolSet<FieldVariableSymbol> Fields => !_fields.IsDefault ? _fields : throw new InvalidOperationException("Fields is not initialized.");
		private SymbolSet<FieldVariableSymbol> _fields;

		public OrderedSymbolSet<ParameterVariableSymbol> Parameters => !_parameters.IsDefault ? _parameters : throw new InvalidOperationException("Parameters is not initialized.");
		private OrderedSymbolSet<ParameterVariableSymbol> _parameters;

		public FunctionBlockSymbol(SourcePosition declaringPosition, CaseInsensitiveString name)
		{
			DeclaringPosition = declaringPosition;
			Name = name;
			_layoutHelper = new StructuredLayoutHelper();
		}

		public T Accept<T, TContext>(IType.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
		public LayoutInfo LayoutInfo => throw new NotImplementedException();

		internal void _SetFields(SymbolSet<FieldVariableSymbol> fields)
		{
			if (!_fields.IsDefault)
				throw new InvalidOperationException();
			_fields = fields;
		}
		internal void _SetParameters(OrderedSymbolSet<ParameterVariableSymbol> parameters)
		{
			if (!_parameters.IsDefault)
				throw new InvalidOperationException();
			_parameters = parameters;
		}

		UndefinedLayoutInfo _IDelayedLayoutType.GetLayoutInfo(MessageBag messageBag, SourcePosition position) => _layoutHelper.GetLayoutInfo(
			messageBag,
			position,
			false,
			Fields);

		void _IDelayedLayoutType.RecursiveLayout(MessageBag messageBag, SourcePosition position) => _layoutHelper.GetLayoutInfo(
			messageBag,
			position,
			false,
			Fields);
	}
	
}