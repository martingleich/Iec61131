using Compiler.Messages;
using System;

namespace Compiler.Types
{
    public sealed class StructuredTypeSymbol : _IDelayedLayoutType, IStructuredTypeSymbol
	{
		private readonly StructuredLayoutHelper _layoutHelper;
		public bool IsUnion { get; }
		public CaseInsensitiveString Name => UniqueName.Name;
		public UniqueSymbolId UniqueName { get; }
		public string Code => Name.Original;

		public LayoutInfo LayoutInfo => _layoutHelper.LayoutInfo;
		private SymbolSet<FieldVariableSymbol> _fields;
		public SymbolSet<FieldVariableSymbol> Fields => !_fields.IsDefault ? _fields : throw new InvalidOperationException("Fields is not initialized");
		public SourceSpan DeclaringSpan { get; }

		public StructuredTypeSymbol(
			SourceSpan declaringSpan,
			bool isUnion,
			CaseInsensitiveString module,
			CaseInsensitiveString name,
			SymbolSet<FieldVariableSymbol> fields,
			LayoutInfo layoutInfo)
		{
			DeclaringSpan = declaringSpan;
			IsUnion = isUnion;
			_fields = fields;
			_layoutHelper = new StructuredLayoutHelper(layoutInfo);
			UniqueName = new( module, name);
		}

		public override string ToString() => Name.ToString();

		internal StructuredTypeSymbol(
			SourceSpan declaringSpan,
			bool isUnion,
			CaseInsensitiveString module,
			CaseInsensitiveString name)
		{
			DeclaringSpan = declaringSpan;
			IsUnion = isUnion;
			_layoutHelper = new StructuredLayoutHelper();
			UniqueName = new( module, name);
		}

		public T Accept<T, TContext>(IType.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);

		public UndefinedLayoutInfo GetLayoutInfo(MessageBag messageBag, SourceSpan span) => _layoutHelper.GetLayoutInfo(messageBag, span, IsUnion, Fields);
		public void RecursiveLayout(MessageBag messageBag, SourceSpan span) => _layoutHelper.RecursiveLayout(messageBag, span, IsUnion, Fields);
		internal void InternalSetFields(SymbolSet<FieldVariableSymbol> fields)
		{
			if (!_fields.IsDefault)
				throw new InvalidOperationException();
			_fields = fields;
		}
	}
}