using Compiler.Messages;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Compiler.Types
{
	public sealed class ArrayType : IType, _IDelayedLayoutType
	{
		public readonly struct Range : IEquatable<Range>
		{
			public readonly int LowerBound;
			public readonly int UpperBound;

			public Range(int lowerBound, int upperBound)
			{
				if (upperBound - lowerBound + 1 < 0)
					throw new ArgumentException($"Upperbound({upperBound}) must be in range [lowerBound({lowerBound})-1, inf].", nameof(upperBound));
				LowerBound = lowerBound;
				UpperBound = upperBound;
			}

			public int Size => UpperBound - LowerBound + 1;

			public bool Equals(Range other) => LowerBound == other.LowerBound && UpperBound == other.UpperBound;
			public override bool Equals(object? obj) => throw new NotImplementedException("Use Equals(ArrayRange) instead");
			public override int GetHashCode() => HashCode.Combine(LowerBound, UpperBound);
			public override string ToString() => $"{LowerBound}..{UpperBound}";

			public static bool operator ==(Range left, Range right)
			{
				return left.Equals(right);
			}

			public static bool operator !=(Range left, Range right)
			{
				return !(left == right);
			}
		}

		private readonly ArrayTypeSyntax? MaybeSyntax;
		private readonly IScope? MaybeScope;

		public CaseInsensitiveString Name => ToString().ToCaseInsensitive();
		public readonly IType BaseType;
		public ImmutableArray<Range> Ranges { get; private set; }
		public UndefinedLayoutInfo? MaybeLayoutInfo { get; private set; }
		public LayoutInfo LayoutInfo => MaybeLayoutInfo!.Value.TryGet(out var result) ? result : LayoutInfo.Zero;
		public int ElementCount => Ranges.Aggregate(1, (x, r) => x * r.Size);
		public string Code => $"ARRAY[{string.Join(", ", Ranges)}] OF {BaseType.Code}";

		public ArrayType(IType baseType, ImmutableArray<Range> ranges)
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
		void _IDelayedLayoutType.RecursiveLayout(MessageBag messageBag, SourcePosition position)
		{
			((_IDelayedLayoutType)this).GetLayoutInfo(messageBag, position);
			DelayedLayoutType.RecursiveLayout(BaseType, messageBag, MaybeSyntax!.BaseType.SourcePosition);
		}
		UndefinedLayoutInfo _IDelayedLayoutType.GetLayoutInfo(MessageBag messageBag, SourcePosition position)
		{
			if (MaybeLayoutInfo is UndefinedLayoutInfo layoutInfo)
				return layoutInfo;
			Ranges = CalculateArrayRanges(MaybeScope!, messageBag, MaybeSyntax!, out var isValid);
			var baseTypeLayout = DelayedLayoutType.GetLayoutInfo(BaseType, messageBag, MaybeSyntax!.BaseType.SourcePosition);
			if (baseTypeLayout.TryGet(out var x) && isValid)
				MaybeLayoutInfo = LayoutInfo.Array(x, ElementCount);
			else
				MaybeLayoutInfo = UndefinedLayoutInfo.Undefined;
			return MaybeLayoutInfo.Value;
		}

		private struct BoundRange<T> where T : class, ILiteralValue
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
		private static Range BoundRangeToArrayRange(MessageBag messageBag, BoundRange<DIntLiteralValue> range)
		{
			if (range.Lower == null && range.Upper != null)
				return new Range(range.Upper.Value, range.Upper.Value);
			else if (range.Lower != null && range.Upper == null)
				return new Range(range.Lower.Value, range.Lower.Value);
			else if (range.Lower != null && range.Upper != null)
				if (range.Upper.Value - range.Lower.Value + 1 < 0)
				{
					messageBag.Add(new InvalidArrayRangesMessages(range.Syntax.SourcePosition));
					return new Range(range.Lower.Value, range.Lower.Value);
				}
				else
					return new Range(range.Lower.Value, range.Upper.Value);
			else
				return new Range(0, 0);
		}
		private static ImmutableArray<Range> CalculateArrayRanges(IScope scope, MessageBag messageBag, ArrayTypeSyntax arraySyntax, out bool isValid)
		{
			var builder = ImmutableArray.CreateBuilder<Range>();
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
	}
}
