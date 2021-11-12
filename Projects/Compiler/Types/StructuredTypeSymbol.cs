using Compiler.Messages;
using System;

namespace Compiler.Types
{
	public sealed class StructuredTypeSymbol : ITypeSymbol, _IDelayedLayoutType
	{
		private StructuredLayoutHelper _layoutHelper;
		public bool IsUnion { get; }
		public CaseInsensitiveString Name { get; }
		public string Code => Name.Original;

		public LayoutInfo LayoutInfo => _layoutHelper.LayoutInfo;
		private SymbolSet<FieldVariableSymbol> _fields;
		public SymbolSet<FieldVariableSymbol> Fields => !_fields.IsDefault ? _fields : throw new InvalidOperationException("Fields is not initialized");
		public SourcePosition DeclaringPosition { get; }

		public StructuredTypeSymbol(
			SourcePosition declaringPosition,
			bool isUnion,
			CaseInsensitiveString name,
			SymbolSet<FieldVariableSymbol> fields,
			LayoutInfo layoutInfo)
		{
			DeclaringPosition = declaringPosition;
			IsUnion = isUnion;
			Name = name;
			_fields = fields;
			_layoutHelper = new StructuredLayoutHelper(layoutInfo);
		}

		public override string ToString() => Name.ToString();

		internal StructuredTypeSymbol(
			SourcePosition declaringPosition,
			bool isUnion,
			CaseInsensitiveString name)
		{
			DeclaringPosition = declaringPosition;
			IsUnion = isUnion;
			Name = name;
			_layoutHelper = new StructuredLayoutHelper();
		}

		public T Accept<T, TContext>(IType.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);

		public UndefinedLayoutInfo GetLayoutInfo(MessageBag messageBag, SourcePosition position) => _layoutHelper.GetLayoutInfo(messageBag, position, IsUnion, Fields);
		public void RecursiveLayout(MessageBag messageBag, SourcePosition position) => _layoutHelper.RecursiveLayout(messageBag, position, IsUnion, Fields);
		internal void InternalSetFields(SymbolSet<FieldVariableSymbol> fields)
		{
			if (!_fields.IsDefault)
				throw new InvalidOperationException();
			_fields = fields;
		}
	}
}