using Compiler.Messages;
using System;

namespace Compiler.Types
{
	public sealed class FunctionBlockSymbol : ICallableTypeSymbol, _IDelayedLayoutType, IStructuredTypeSymbol
	{
		public string Code => Name.Original;
		public SourceSpan DeclaringSpan { get; }
		public CaseInsensitiveString Name => UniqueName.Name;
		public UniqueSymbolId UniqueName { get; }

		private readonly StructuredLayoutHelper _layoutHelper;
		public SymbolSet<FieldVariableSymbol> Fields => !_fields.IsDefault ? _fields : throw new InvalidOperationException("Fields is not initialized.");
		private SymbolSet<FieldVariableSymbol> _fields;

		public OrderedSymbolSet<ParameterVariableSymbol> Parameters => !_parameters.IsDefault ? _parameters : throw new InvalidOperationException("Parameters is not initialized.");
		private OrderedSymbolSet<ParameterVariableSymbol> _parameters;

		public FunctionBlockSymbol(SourceSpan declaringSpan, CaseInsensitiveString module, CaseInsensitiveString name)
		{
			DeclaringSpan = declaringSpan;
			_layoutHelper = new StructuredLayoutHelper();
			UniqueName = new UniqueSymbolId(module, name);
		}

		public T Accept<T, TContext>(IType.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
		public LayoutInfo LayoutInfo => _layoutHelper.LayoutInfo;

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

		UndefinedLayoutInfo _IDelayedLayoutType.GetLayoutInfo(MessageBag messageBag, SourceSpan span) => _layoutHelper.GetLayoutInfo(
			messageBag,
			span,
			false,
			Fields);

		void _IDelayedLayoutType.RecursiveLayout(MessageBag messageBag, SourceSpan span) => _layoutHelper.GetLayoutInfo(
			messageBag,
			span,
			false,
			Fields);
		public override string ToString() => UniqueName.ToString();
    }
}