using StandardLibraryExtensions;
using System;

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

		public static LayoutInfo Union(LayoutInfo a, LayoutInfo b) => new (
				Math.Max(a.Size, b.Size),
				MathExtensions.Lcm(a.Alignment, b.Alignment));
		public static LayoutInfo Array(LayoutInfo element, int count) => new(element.Size * count, element.Alignment);

		public static bool operator ==(LayoutInfo left, LayoutInfo right) => left.Equals(right);
		public static bool operator !=(LayoutInfo left, LayoutInfo right) => !(left == right);
	}

	public readonly struct FieldLayout
	{
		public readonly int OwnerAlignment;
		public readonly int Offset;
		public readonly int Size;

		public static readonly FieldLayout Zero = new(1, 0, 0);

        public FieldLayout(int ownerAlignment, int offset, int size)
        {
            OwnerAlignment = ownerAlignment;
            Offset = offset;
            Size = size;
        }

		public FieldLayout NextField(LayoutInfo typeLayout)
		{
			int cursor = Offset + Size;
            if (cursor % typeLayout.Alignment != 0)
                cursor = ((cursor / typeLayout.Alignment) + 1) * typeLayout.Alignment;
            var alignment = MathExtensions.Lcm(OwnerAlignment, typeLayout.Alignment);
			return new(alignment, cursor, typeLayout.Size);
		}
		public LayoutInfo ToTypeLayout()
		{
			int cursor = Offset + Size;
			if (cursor % OwnerAlignment != 0)
				cursor = ((cursor / OwnerAlignment) + 1) * OwnerAlignment;
			return new(cursor, OwnerAlignment);
		}
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
