using StandardLibraryExtensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Compiler
{
	public readonly struct LayoutInfo : IEquatable<LayoutInfo>
	{
		public readonly int Size;
		public readonly int Alignment;

		public static readonly LayoutInfo Zero = new(0, 1);

		public LayoutInfo(int size, int alignment)
		{
			if (size < 0)
				throw new ArgumentException("Size must be non-negative", nameof(size));
			if (alignment <= 0)
				throw new ArgumentException("Size must greater than zero", nameof(alignment));
			Size = size;
			Alignment = alignment;
		}

		public bool Equals(LayoutInfo other) => other.Size == Size && other.Alignment == Alignment;
		public override bool Equals(object? obj) => obj is LayoutInfo other && Equals(other);
		public override int GetHashCode() => HashCode.Combine(Size, Alignment);
		public override string ToString() => $"Size = {Size}";

		public static LayoutInfo Union(IEnumerable<LayoutInfo> layouts) => layouts.Aggregate(Union);
		public static LayoutInfo Union(LayoutInfo a, LayoutInfo b) => new (
				Math.Max(a.Size, b.Size),
				MathExtensions.Lcm(a.Alignment, b.Alignment));
		public static LayoutInfo Array(LayoutInfo element, int count) => new(element.Size * count, element.Alignment);
		public static LayoutInfo Struct(IEnumerable<LayoutInfo> fieldLayouts)
		{
			int alignment = 1;
			int cursor = 0;
			foreach (var f in fieldLayouts.OrderByDescending(f => f.Alignment))
			{
				if (cursor % f.Alignment != 0)
					cursor = ((cursor / f.Alignment) + 1) * f.Alignment;
				alignment = MathExtensions.Lcm(alignment, f.Alignment);
				cursor += f.Size;
			}

			if (cursor % alignment != 0)
				cursor = ((cursor / alignment) + 1) * alignment;

			return new LayoutInfo(cursor, alignment);
		}

		public static bool operator ==(LayoutInfo left, LayoutInfo right) => left.Equals(right);
		public static bool operator !=(LayoutInfo left, LayoutInfo right) => !(left == right);
	}

	public readonly struct UndefinedLayoutInfo
	{
		public static readonly UndefinedLayoutInfo Undefined = default;
		private readonly LayoutInfo? Value;

		private UndefinedLayoutInfo(LayoutInfo value)
		{
			Value = value;
		}

		public static implicit operator UndefinedLayoutInfo(LayoutInfo info) => new (info);

		public bool TryGet(out LayoutInfo value)
		{
			if (Value is LayoutInfo result)
			{
				value = result;
				return true;
			}
			else
			{
				value = LayoutInfo.Zero;
				return false;
			}
		}
	}
}
