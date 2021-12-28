using Compiler.Messages;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Compiler.Types
{
	public sealed class StructuredLayoutHelper
	{
		private UndefinedLayoutInfo? MaybeLayoutInfo;
		public LayoutInfo LayoutInfo => MaybeLayoutInfo!.Value.TryGet(out var result) ? result : LayoutInfo.Zero;


		private bool RecursiveLayoutWasDone;
		private bool Inside_RecusiveLayout;
		public void RecursiveLayout<T>(
			MessageBag messageBag, 
			SourceSpan span,
			bool isUnion,
			SymbolSet<T> fields) where T : IVariableSymbol
		{
			if (RecursiveLayoutWasDone)
				return;

			GetLayoutInfo(messageBag, span, isUnion, fields);

			if (!Inside_RecusiveLayout)
			{
				Inside_RecusiveLayout = true;
				foreach (var field in fields)
					DelayedLayoutType.RecursiveLayout(field.Type, messageBag, field.DeclaringSpan);
				Inside_RecusiveLayout = false;
			}
			RecursiveLayoutWasDone = true;
		}
		private bool Inside_GetLayoutInfo;

		public StructuredLayoutHelper(LayoutInfo layoutInfo)
		{
			MaybeLayoutInfo = layoutInfo;
			RecursiveLayoutWasDone = true;
		}

		public StructuredLayoutHelper()
		{
		}

		public UndefinedLayoutInfo GetLayoutInfo<T>(
			MessageBag messageBag,
			SourceSpan span,
			bool isUnion,
			SymbolSet<T> fields) where T : IVariableSymbol
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
					var fieldLayouts = new LayoutInfo[fields.Count];
					int i = 0;
					foreach (var field in fields)
					{
						var undefinedLayoutInfo = DelayedLayoutType.GetLayoutInfo(field.Type, messageBag, field.DeclaringSpan);
						if (undefinedLayoutInfo.TryGet(out var layoutInfo))
						{
							fieldLayouts[i] = layoutInfo;
							++i;
						}
						else
							isUndefined = true;
					}
					Inside_GetLayoutInfo = false;
					if (isUndefined)
					{
						messageBag.Add(new TypeNotCompleteMessage(span));
						MaybeLayoutInfo = LayoutInfo.Zero;
					}
					else
					{
						MaybeLayoutInfo = isUnion ? LayoutInfo.Union(fieldLayouts) : LayoutInfo.Struct(fieldLayouts);
					}
				}
			}
			return MaybeLayoutInfo.Value;
		}

	}
}