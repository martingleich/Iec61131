using Compiler.Messages;
using System;

namespace Compiler.Types
{
	public sealed class AliasTypeSymbol : ITypeSymbol, _IDelayedLayoutType
	{
		public SourcePosition DeclaringPosition { get; }
		public CaseInsensitiveString Name { get; }
		public IType? _aliasedType;
		private bool Inside_GetLayoutInfo;
		private bool RecursiveLayoutWasDone;
		private bool Inside_RecusiveLayout;

		public IType AliasedType => _aliasedType ?? throw new InvalidOperationException();

		public AliasTypeSymbol(SourcePosition declaringPosition, CaseInsensitiveString name)
		{
			DeclaringPosition = declaringPosition;
			Name = name;
			_aliasedType = null;
		}
		public AliasTypeSymbol(SourcePosition declaringPosition, CaseInsensitiveString name, IType aliasedType)
		{
			DeclaringPosition = declaringPosition;
			Name = name;
			_aliasedType = aliasedType ?? throw new ArgumentNullException(nameof(aliasedType));
			MaybeLayoutInfo = aliasedType.LayoutInfo;
			RecursiveLayoutWasDone = false;
		}

		private UndefinedLayoutInfo? MaybeLayoutInfo { get; set; }
		public LayoutInfo LayoutInfo => MaybeLayoutInfo!.Value.TryGet(out var result) ? result : LayoutInfo.Zero;
		public string Code => Name.Original;

		public T Accept<T, TContext>(IType.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);

		internal void _SetAliasedType(IType aliasedType)
		{
			if (aliasedType is null)
				throw new ArgumentNullException(nameof(aliasedType));
			if (_aliasedType != null)
				throw new InvalidOperationException();
			_aliasedType = aliasedType;
		}

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
					Inside_GetLayoutInfo = true;
					var undefinedLayoutInfo = DelayedLayoutType.GetLayoutInfo(_aliasedType!, messageBag, position);
					Inside_GetLayoutInfo = false;
					if (undefinedLayoutInfo.TryGet(out var layoutInfo))
					{
						MaybeLayoutInfo = layoutInfo;
					}
					else
					{
						MaybeLayoutInfo = LayoutInfo.Zero;
						messageBag.Add(new TypeNotCompleteMessage(DeclaringPosition));
					}
				}
		
			}

			return MaybeLayoutInfo.Value;
		}

		void _IDelayedLayoutType.RecursiveLayout(MessageBag messageBag, SourcePosition position)
		{
			if (RecursiveLayoutWasDone)
				return;
			((_IDelayedLayoutType)this).GetLayoutInfo(messageBag, position);

			if (!Inside_RecusiveLayout)
			{
				Inside_RecusiveLayout = true;
				DelayedLayoutType.RecursiveLayout(_aliasedType!, messageBag, position);
				Inside_RecusiveLayout = false;
			}
			RecursiveLayoutWasDone = true;
		}
	}
}