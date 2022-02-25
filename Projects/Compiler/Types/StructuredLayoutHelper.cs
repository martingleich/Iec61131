using Compiler.Messages;
using StandardLibraryExtensions;
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
		public void RecursiveLayout(
			MessageBag messageBag, 
			SourceSpan span,
			bool isUnion,
			SymbolSet<FieldVariableSymbol> fields)
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

		public UndefinedLayoutInfo GetLayoutInfo(
			MessageBag messageBag,
			SourceSpan span,
			bool isUnion,
			SymbolSet<FieldVariableSymbol> fields)
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
					var fieldLayouts = new (LayoutInfo, FieldVariableSymbol)[fields.Count];
					int i = 0;
					foreach (var field in fields)
					{
						var undefinedLayoutInfo = DelayedLayoutType.GetLayoutInfo(field.Type, messageBag, field.DeclaringSpan);
						if (undefinedLayoutInfo.TryGet(out var layoutInfo))
						{
							fieldLayouts[i] = (layoutInfo, field);
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
						if (fieldLayouts.Length > 0)
						{
							if (isUnion)
							{
								LayoutInfo layoutResult = fieldLayouts[0].Item1;
								fieldLayouts[0].Item2._Complete(0);
								for(int j = 1; j < fieldLayouts.Length; ++j)
								{
									var fieldLayout = fieldLayouts[j];
									layoutResult = LayoutInfo.Union(layoutResult, fieldLayout.Item1);
									fieldLayout.Item2._Complete(0);
								}
								MaybeLayoutInfo = layoutResult;
							}
							else
							{
								Array.Sort(fieldLayouts, (a, b) => a.Item1.Alignment.CompareTo(b.Item1.Alignment));
								int alignment = 1;
								int cursor = 0;
								foreach (var fl in fieldLayouts)
								{
									var f = fl.Item1;
									if (cursor % f.Alignment != 0)
										cursor = ((cursor / f.Alignment) + 1) * f.Alignment;
									alignment = MathExtensions.Lcm(alignment, f.Alignment);
									fl.Item2._Complete(cursor);
									cursor += f.Size;
								}

								if (cursor % alignment != 0)
									cursor = ((cursor / alignment) + 1) * alignment;

								MaybeLayoutInfo = new LayoutInfo(cursor, alignment);
							}
						}
						else
						{
							MaybeLayoutInfo = LayoutInfo.Zero;
						}
					}
				}
			}
			return MaybeLayoutInfo.Value;
		}

	}
}