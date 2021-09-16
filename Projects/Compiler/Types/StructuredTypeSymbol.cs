using Compiler.Messages;
using System;
using System.Collections.Generic;

namespace Compiler.Types
{
	public sealed class StructuredTypeSymbol : ITypeSymbol, _IDelayedLayoutType
	{
		public bool IsUnion { get; }
		public CaseInsensitiveString Name { get; }
		public string Code => Name.Original;

		public LayoutInfo? MaybeLayoutInfo { get; private set; }
		public LayoutInfo LayoutInfo => MaybeLayoutInfo!.Value;

		private SymbolSet<FieldSymbol> _fields;
		public SymbolSet<FieldSymbol> Fields => !_fields.IsDefault ? _fields : throw new InvalidOperationException("Fields is not initialized");
		public SourcePosition DeclaringPosition { get; }

		public StructuredTypeSymbol(
			SourcePosition declaringPosition,
			bool isUnion,
			CaseInsensitiveString name,
			SymbolSet<FieldSymbol> fields,
			LayoutInfo layoutInfo)
		{
			DeclaringPosition = declaringPosition;
			IsUnion = isUnion;
			Name = name;
			_fields = fields;
			MaybeLayoutInfo = layoutInfo;
			HasRecusiveLayout = true;
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
			HasRecusiveLayout = false;
		}
		internal void _SetFields(SymbolSet<FieldSymbol> fields)
		{
			if (!_fields.IsDefault)
				throw new InvalidOperationException();
			_fields = fields;
		}

		private bool HasRecusiveLayout;
		private bool Inside_RecusiveLayout;
		LayoutInfo _IDelayedLayoutType.RecursiveLayout(MessageBag messageBag, SourcePosition position)
		{
			if (HasRecusiveLayout)
				return LayoutInfo;

			var layoutInfo = ((_IDelayedLayoutType)this).GetLayoutInfo(messageBag, position);

			if (!Inside_RecusiveLayout)
			{
				Inside_RecusiveLayout = true;
				foreach (var field in _fields)
					DelayedLayoutType.RecursiveLayout(field.Type, messageBag, field.DeclaringPosition);
				Inside_RecusiveLayout = false;
			}
			HasRecusiveLayout = true;
			return layoutInfo;
		}
		private bool Inside_GetLayoutInfo;
		LayoutInfo _IDelayedLayoutType.GetLayoutInfo(MessageBag messageBag, SourcePosition position)
		{
			if (!MaybeLayoutInfo.HasValue)
				if (Inside_GetLayoutInfo)
				{
					messageBag.Add(new TypeNotCompleteMessage(position));
					return LayoutInfo.Zero;
				}
				else
				{
					Inside_GetLayoutInfo = true;
					var fieldLayouts = new List<LayoutInfo>();
					foreach (var field in _fields)
					{
						var layoutInfo = DelayedLayoutType.GetLayoutInfo(field.Type, messageBag, field.DeclaringPosition);
						fieldLayouts.Add(layoutInfo);
					}
					Inside_GetLayoutInfo = false;
					MaybeLayoutInfo = IsUnion ? LayoutInfo.Union(fieldLayouts) : LayoutInfo.Struct(fieldLayouts);
				}
			return MaybeLayoutInfo.Value;
		}

		public T Accept<T, TContext>(IType.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
	}
}