using Compiler.CodegenIR;
using Compiler.Messages;
using Compiler.Scopes;
using Runtime.IR.RuntimeTypes;
using System;
using System.Collections.Immutable;

namespace Compiler.Types
{
	public sealed class ArrayType : IType, _IDelayedLayoutType
	{
		private readonly ArrayTypeSyntax? MaybeSyntax;
		private readonly IScope? MaybeScope;

		public readonly IType BaseType;
		public ImmutableArray<ArrayTypeRange> Ranges { get; private set; }
		public UndefinedLayoutInfo? MaybeLayoutInfo { get; private set; }
		public LayoutInfo LayoutInfo => MaybeLayoutInfo!.Value.TryGet(out var result) ? result : LayoutInfo.Zero;
		public int ElementCount
		{
			get
			{
				int size = 1;
				foreach (var r in Ranges)
					size *= r.Size;
				return size;
			}
		}
		public string Code => $"ARRAY[{string.Join(", ", Ranges)}] OF {BaseType.Code}";

		public bool IsEmpty
		{
			get
			{
				foreach (var r in Ranges)
					if (r.Size == 0)
						return true;
				return false;
			}
		}

		public ArrayType(IType baseType, ImmutableArray<ArrayTypeRange> ranges)
		{
			BaseType = baseType ?? throw new ArgumentNullException(nameof(baseType));
			Ranges = ranges;
			MaybeLayoutInfo = LayoutInfo.Array(BaseType.LayoutInfo, ElementCount);
		}

		public override string ToString() => Code;

		internal ArrayType(IType baseType, IScope scope, ArrayTypeSyntax declaringSyntax)
		{
			MaybeSyntax = declaringSyntax ?? throw new ArgumentNullException(nameof(declaringSyntax));
			MaybeScope = scope ?? throw new ArgumentNullException(nameof(scope));
			BaseType = baseType ?? throw new ArgumentNullException(nameof(baseType));
		}
		void _IDelayedLayoutType.RecursiveLayout(MessageBag messageBag, SourceSpan span)
		{
			((_IDelayedLayoutType)this).GetLayoutInfo(messageBag, span);
			DelayedLayoutType.RecursiveLayout(BaseType, messageBag, MaybeSyntax!.BaseType.SourceSpan);
		}
		UndefinedLayoutInfo _IDelayedLayoutType.GetLayoutInfo(MessageBag messageBag, SourceSpan span)
		{
			if (MaybeLayoutInfo is UndefinedLayoutInfo layoutInfo)
				return layoutInfo;
			Ranges = CalculateArrayRanges(MaybeScope!, messageBag, MaybeSyntax!, out var isValid);
			var baseTypeLayout = DelayedLayoutType.GetLayoutInfo(BaseType, messageBag, MaybeSyntax!.BaseType.SourceSpan);
			if (baseTypeLayout.TryGet(out var x) && isValid)
				MaybeLayoutInfo = LayoutInfo.Array(x, ElementCount);
			else
				MaybeLayoutInfo = UndefinedLayoutInfo.Undefined;
			return MaybeLayoutInfo.Value;
		}

		private readonly struct BoundRange<T> where T : class, ILiteralValue
		{
			public readonly RangeSyntax Syntax;
			public readonly T? Lower;
			public readonly T? Upper;

			public BoundRange(T? lower, T? upper, RangeSyntax syntax)
			{
				Lower = lower;
				Upper = upper;
				Syntax = syntax ?? throw new ArgumentNullException(nameof(syntax));
			}
		}
		private static BoundRange<T> BindRange<T>(IScope scope, MessageBag messageBag, RangeSyntax syntax, IType type) where T : class, ILiteralValue
		{
			var lowerbound = ConstantExpressionEvaluator.EvaluateConstant(scope, messageBag, type, syntax.LowerBound) as T;
			var upperbound = ConstantExpressionEvaluator.EvaluateConstant(scope, messageBag, type, syntax.UpperBound) as T;
			return new BoundRange<T>(lowerbound, upperbound, syntax);
		}
		private static ArrayTypeRange BoundRangeToArrayRange(MessageBag messageBag, BoundRange<DIntLiteralValue> range)
		{
			if (range.Lower == null && range.Upper != null)
				return new ArrayTypeRange(range.Upper.Value, range.Upper.Value);
			else if (range.Lower != null && range.Upper == null)
				return new ArrayTypeRange(range.Lower.Value, range.Lower.Value);
			else if (range.Lower != null && range.Upper != null)
				if (range.Upper.Value - range.Lower.Value + 1 < 0)
				{
					messageBag.Add(new InvalidArrayRangesMessage(range.Syntax.SourceSpan));
					return new ArrayTypeRange(range.Lower.Value, range.Lower.Value);
				}
				else
					return new ArrayTypeRange(range.Lower.Value, range.Upper.Value);
			else
				return new ArrayTypeRange(0, 0);
		}
		private static ImmutableArray<ArrayTypeRange> CalculateArrayRanges(IScope scope, MessageBag messageBag, ArrayTypeSyntax arraySyntax, out bool isValid)
		{
			var builder = ImmutableArray.CreateBuilder<ArrayTypeRange>();
			isValid = true;
			foreach (var rangeSyntax in arraySyntax.Ranges)
			{
				var boundRange = BindRange<DIntLiteralValue>(scope, messageBag, rangeSyntax, scope.SystemScope.DInt);
				isValid &= boundRange.Lower != null && boundRange.Upper != null;
				var range =  BoundRangeToArrayRange(messageBag, boundRange);
				builder.Add(range);
			}
			return builder.ToImmutable();
		}

		public T Accept<T, TContext>(IType.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);

		public RuntimeTypeArray GetRuntimeType(RuntimeTypeFactory runtimeTypeFactory)
		{
            return new RuntimeTypeArray(Ranges, runtimeTypeFactory.GetRuntimeType(BaseType));
		}

        public int? GetIndexOf(ImmutableArray<int> indices)
        {
			if (indices.Length != Ranges.Length)
				throw new ArgumentException($"{nameof(indices.Length)}({indices.Length} must be equal to {nameof(Ranges.Length)}({Ranges.Length})");
			int index = 0;
			int scale = 1;
			for (int i = indices.Length - 1; i >= 0; --i)
			{
				var range = Ranges[i];
				var idx = indices[i];
				if (!range.IsInRange(idx))
					return null;
				var offset = idx - range.LowerBound;
				index += scale * offset;
				scale *= range.Size;
			}
			return index;
        }
    }
}
