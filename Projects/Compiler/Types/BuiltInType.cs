using Compiler.CodegenIR;
using Runtime.IR.RuntimeTypes;
using System;

namespace Compiler.Types
{
	public sealed class BuiltInType : IType, IEquatable<BuiltInType>
	{
		[Flags]
		public enum Flag
		{
			None = 0,
			UInt = 1,
			SInt = 2,
			Real = 3,
			Mask = 3,
		}

		public CaseInsensitiveString Name { get; }
		public string Code => Name.ToString();
		public IRuntimeType? RuntimeType { get; }
		public BuiltInType(int size, int alignment, string name, Flag flags = Flag.None)
		{
			Name = name.ToCaseInsensitive();
			LayoutInfo = new LayoutInfo(size, alignment);
			Flags = flags;
		}
		public BuiltInType(IRuntimeType runtimeType, int alignment, Flag flags = Flag.None)
		{
			Name = runtimeType.Name.ToCaseInsensitive();
			LayoutInfo = new LayoutInfo(runtimeType.Size, alignment);
			Flags = flags;
			RuntimeType = runtimeType;
		}

		public int Size => LayoutInfo.Size;
		public int Alignment => LayoutInfo.Alignment;
		public bool IsArithmetic => Flags != 0;
		public bool IsUnsignedInt => (Flags & Flag.Mask) == Flag.UInt;
		public bool IsSignedInt => (Flags & Flag.Mask) == Flag.SInt;
		public bool IsInt => IsUnsignedInt || IsSignedInt;
		public bool IsReal => (Flags & Flag.Mask) == Flag.Real;
		public Flag Flags { get; }

		public LayoutInfo LayoutInfo { get; }

		public T Accept<T, TContext>(IType.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);

		public override string ToString() => Name.ToString();

		public bool Equals(BuiltInType? other) => other != null && other.Name == Name;
		public override int GetHashCode() => Name.GetHashCode();
		public override bool Equals(object? obj) => throw new NotImplementedException();

		public IRuntimeType GetRuntimeType(RuntimeTypeFactory factory)
		{
			if (RuntimeType is IRuntimeType runtimeType)
				return runtimeType;
			else
				return new RuntimeTypeUnknown(Code, Size);
		}
	}
}