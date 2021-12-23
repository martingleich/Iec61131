using Compiler.Messages;
using System;

namespace Compiler.Types
{
	public sealed class AliasTypeSymbol : ITypeSymbol, _IDelayedLayoutType, IScopeSymbol
	{
		public SourceSpan DeclaringSpan { get; }
		public CaseInsensitiveString Name => UniqueId.Name;
		public UniqueSymbolId UniqueId { get; }
		public IType? _aliasedType;
		private bool Inside_GetLayoutInfo;
		private bool RecursiveLayoutWasDone;
		private bool Inside_RecusiveLayout;

		public IType AliasedType => _aliasedType ?? throw new InvalidOperationException();

		public AliasTypeSymbol(SourceSpan declaringSpan, CaseInsensitiveString module, CaseInsensitiveString name)
		{
			DeclaringSpan = declaringSpan;
			_aliasedType = null;
			UniqueId = new UniqueSymbolId(module, name);
		}
		public AliasTypeSymbol(SourceSpan declaringSpan, CaseInsensitiveString module, CaseInsensitiveString name, IType aliasedType)
		{
			DeclaringSpan = declaringSpan;
			_aliasedType = aliasedType ?? throw new ArgumentNullException(nameof(aliasedType));
			MaybeLayoutInfo = aliasedType.LayoutInfo;
			RecursiveLayoutWasDone = false;
			UniqueId = new UniqueSymbolId(module, name);
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

		UndefinedLayoutInfo _IDelayedLayoutType.GetLayoutInfo(MessageBag messageBag, SourceSpan span)
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
					var undefinedLayoutInfo = DelayedLayoutType.GetLayoutInfo(_aliasedType!, messageBag, span);
					Inside_GetLayoutInfo = false;
					if (undefinedLayoutInfo.TryGet(out var layoutInfo))
					{
						MaybeLayoutInfo = layoutInfo;
					}
					else
					{
						MaybeLayoutInfo = LayoutInfo.Zero;
						messageBag.Add(new TypeNotCompleteMessage(DeclaringSpan));
					}
				}
		
			}

			return MaybeLayoutInfo.Value;
		}

		void _IDelayedLayoutType.RecursiveLayout(MessageBag messageBag, SourceSpan span)
		{
			if (RecursiveLayoutWasDone)
				return;
			((_IDelayedLayoutType)this).GetLayoutInfo(messageBag, span);

			if (!Inside_RecusiveLayout)
			{
				Inside_RecusiveLayout = true;
				DelayedLayoutType.RecursiveLayout(_aliasedType!, messageBag, span);
				Inside_RecusiveLayout = false;
			}
			RecursiveLayoutWasDone = true;
		}

		public ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourceSpan errorPosition)
		{
			if (AliasedType is IScopeSymbol aliasedScope)
				return aliasedScope.LookupVariable(identifier, errorPosition);
			else
				return EmptyScopeHelper.LookupVariable(Name, identifier, errorPosition);
		}
		public ErrorsAnd<IScopeSymbol> LookupScope(CaseInsensitiveString identifier, SourceSpan errorPosition)
			=> EmptyScopeHelper.LookupScope(Name, identifier, errorPosition);
		public ErrorsAnd<ITypeSymbol> LookupType(CaseInsensitiveString identifier, SourceSpan errorPosition)
			=> EmptyScopeHelper.LookupType(Name, identifier, errorPosition);
		public override string ToString() => UniqueId.ToString();
	}
}