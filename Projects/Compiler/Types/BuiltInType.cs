using System;

namespace Compiler.Types
{
	public sealed class BuiltInType : IType, IEquatable<BuiltInType>
	{
		[Flags]
		public enum Flag
		{
			None = 0,
			Arithmetic = 1,
			Unsigned = 2,
		}


		public CaseInsensitiveString Name { get; }
		public string Code => Name.ToString();
		public BuiltInType(int size, int alignment, string name, Flag flags = Flag.None)
		{
			Name = name.ToCaseInsensitive();
			Size = size;
			Alignment = alignment;
			Flags = flags;
		}

		public int Size { get; }
		public int Alignment { get; }
		public bool IsArithmetic => (Flags & Flag.Arithmetic) != 0;
		public bool IsUnsigned => (Flags & Flag.Unsigned) != 0;
		public Flag Flags { get; }

		public LayoutInfo LayoutInfo => new(Size, Alignment);

		public T Accept<T, TContext>(IType.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);

		public override string ToString() => Name.ToString();

		public bool Equals(BuiltInType? other) => other != null && other.Name == Name;
		public override int GetHashCode() => Name.GetHashCode();
		public override bool Equals(object? obj) => throw new NotImplementedException();

	}
}