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

		private UndefinedLayoutInfo? MaybeLayoutInfo { get; set; }
		public LayoutInfo LayoutInfo => MaybeLayoutInfo!.Value.TryGet(out var result) ? result : LayoutInfo.Zero;

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
			MaybeLayoutInfo = layoutInfo;
			RecursiveLayoutWasDone = true;
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
			RecursiveLayoutWasDone = false;
		}
		internal void _SetFields(SymbolSet<FieldVariableSymbol> fields)
		{
			if (!_fields.IsDefault)
				throw new InvalidOperationException();
			_fields = fields;
		}

		private bool RecursiveLayoutWasDone;
		private bool Inside_RecusiveLayout;
		void _IDelayedLayoutType.RecursiveLayout(MessageBag messageBag, SourcePosition position)
		{
			if (RecursiveLayoutWasDone)
				return;

			((_IDelayedLayoutType)this).GetLayoutInfo(messageBag, position);

			if (!Inside_RecusiveLayout)
			{
				Inside_RecusiveLayout = true;
				foreach (var field in _fields)
					DelayedLayoutType.RecursiveLayout(field.Type, messageBag, field.DeclaringPosition);
				Inside_RecusiveLayout = false;
			}
			RecursiveLayoutWasDone = true;
		}
		private bool Inside_GetLayoutInfo;
		UndefinedLayoutInfo _IDelayedLayoutType.GetLayoutInfo(MessageBag messageBag, SourcePosition position)
		{
			if (!MaybeLayoutInfo.HasValue)
			{
				if (Inside_GetLayoutInfo)
				{
					MaybeLayoutInfo = UndefinedLayoutInfo.Undefined;
				}
				else
				{
					bool isUndefined = false;
					Inside_GetLayoutInfo = true;
					var fieldLayouts = new List<LayoutInfo>();
					foreach (var field in _fields)
					{
						var undefinedLayoutInfo = DelayedLayoutType.GetLayoutInfo(field.Type, messageBag, field.DeclaringPosition);
						if (undefinedLayoutInfo.TryGet(out var layoutInfo))
							fieldLayouts.Add(layoutInfo);
						else
							isUndefined = true;
					}
					Inside_GetLayoutInfo = false;
					MaybeLayoutInfo = IsUnion ? LayoutInfo.Union(fieldLayouts) : LayoutInfo.Struct(fieldLayouts);
					if(isUndefined == true)
						messageBag.Add(new TypeNotCompleteMessage(DeclaringPosition));
				}
			}
			return MaybeLayoutInfo.Value;
		}

		public T Accept<T, TContext>(IType.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
	}
}