using Compiler.Types;
using System;

namespace Compiler
{
	public interface ILiteralValue : IEquatable<ILiteralValue>
	{
		public IType Type { get; }
	}

	public interface IAnyIntLiteralValue : ILiteralValue
	{
		public OverflowingInteger Value { get; }
	}

	public sealed class SIntLiteralValue : IAnyIntLiteralValue, IEquatable<SIntLiteralValue>
	{
		public readonly sbyte Value;
		OverflowingInteger IAnyIntLiteralValue.Value => OverflowingInteger.FromLong(Value);

		public SIntLiteralValue(sbyte value, IType type)
		{
			Value = value;
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public IType Type { get; }

		public bool Equals(SIntLiteralValue? other) => other != null && other.Value == Value;
		public bool Equals(ILiteralValue? other) => Equals(other as SIntLiteralValue);
		public override bool Equals(object? obj) => Equals(obj as SIntLiteralValue);
		public override int GetHashCode() => Value.GetHashCode();
		public override string ToString() => $"SINT#{Value}";
	}
	public sealed class USIntLiteralValue : IAnyIntLiteralValue, IEquatable<USIntLiteralValue>
	{
		public readonly byte Value;
		OverflowingInteger IAnyIntLiteralValue.Value => OverflowingInteger.FromLong(Value);

		public USIntLiteralValue(byte value, IType type)
		{
			Value = value;
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public IType Type { get; }

		public bool Equals(USIntLiteralValue? other) => other != null && other.Value == Value;
		public bool Equals(ILiteralValue? other) => Equals(other as USIntLiteralValue);
		public override bool Equals(object? obj) => Equals(obj as USIntLiteralValue);
		public override int GetHashCode() => Value.GetHashCode();
		public override string ToString() => $"USINT#{Value}";
	}
	
	public sealed class UIntLiteralValue : IAnyIntLiteralValue, IEquatable<UIntLiteralValue>
	{
		public readonly uint Value;
		OverflowingInteger IAnyIntLiteralValue.Value => OverflowingInteger.FromLong(Value);

		public UIntLiteralValue(uint value, IType type)
		{
			Value = value;
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public IType Type { get; }

		public bool Equals(UIntLiteralValue? other) => other != null && other.Value == Value;
		public bool Equals(ILiteralValue? other) => Equals(other as UIntLiteralValue);
		public override bool Equals(object? obj) => Equals(obj as UIntLiteralValue);
		public override int GetHashCode() => Value.GetHashCode();
		public override string ToString() => $"UINT#{Value}";
	}
	public sealed class IntLiteralValue : IAnyIntLiteralValue, IEquatable<IntLiteralValue>
	{
		public readonly short Value;
		OverflowingInteger IAnyIntLiteralValue.Value => OverflowingInteger.FromLong(Value);

		public IntLiteralValue(short value, IType type)
		{
			Value = value;
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public IType Type { get; }

		public bool Equals(IntLiteralValue? other) => other != null && other.Value == Value;
		public bool Equals(ILiteralValue? other) => Equals(other as IntLiteralValue);
		public override bool Equals(object? obj) => Equals(obj as IntLiteralValue);
		public override int GetHashCode() => Value.GetHashCode();
		public override string ToString() => $"INT#{Value}";
	}
	
	public sealed class UDIntLiteralValue : IAnyIntLiteralValue, IEquatable<UDIntLiteralValue>
	{
		public readonly uint Value;
		OverflowingInteger IAnyIntLiteralValue.Value => OverflowingInteger.FromLong(Value);

		public UDIntLiteralValue(uint value, IType type)
		{
			Value = value;
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public IType Type { get; }

		public bool Equals(UDIntLiteralValue? other) => other != null && other.Value == Value;
		public bool Equals(ILiteralValue? other) => Equals(other as UDIntLiteralValue);
		public override bool Equals(object? obj) => Equals(obj as UDIntLiteralValue);
		public override int GetHashCode() => Value.GetHashCode();
		public override string ToString() => $"UDINT#{Value}";
	}
	public sealed class DIntLiteralValue : IAnyIntLiteralValue, IEquatable<DIntLiteralValue>
	{
		public readonly int Value;
		OverflowingInteger IAnyIntLiteralValue.Value => OverflowingInteger.FromLong(Value);

		public DIntLiteralValue(int value, IType type)
		{
			Value = value;
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public IType Type { get; }

		public bool Equals(DIntLiteralValue? other) => other != null && other.Value == Value;
		public bool Equals(ILiteralValue? other) => Equals(other as DIntLiteralValue);
		public override bool Equals(object? obj) => Equals(obj as DIntLiteralValue);
		public override int GetHashCode() => Value.GetHashCode();
		public override string ToString() => $"DINT#{Value}";
	}

	public sealed class ULIntLiteralValue : IAnyIntLiteralValue, IEquatable<ULIntLiteralValue>
	{
		public readonly ulong Value;
		OverflowingInteger IAnyIntLiteralValue.Value => OverflowingInteger.FromULong(Value);

		public ULIntLiteralValue(ulong value, IType type)
		{
			Value = value;
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public IType Type { get; }

		public bool Equals(ULIntLiteralValue? other) => other != null && other.Value == Value;
		public bool Equals(ILiteralValue? other) => Equals(other as ULIntLiteralValue);
		public override bool Equals(object? obj) => Equals(obj as ULIntLiteralValue);
		public override int GetHashCode() => Value.GetHashCode();
		public override string ToString() => $"UlINT#{Value}";
	}
	public sealed class LIntLiteralValue : IAnyIntLiteralValue, IEquatable<LIntLiteralValue>
	{
		public readonly long Value;
		OverflowingInteger IAnyIntLiteralValue.Value => OverflowingInteger.FromLong(Value);

		public LIntLiteralValue(long value, IType type)
		{
			Value = value;
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public IType Type { get; }

		public bool Equals(LIntLiteralValue? other) => other != null && other.Value == Value;
		public bool Equals(ILiteralValue? other) => Equals(other as LIntLiteralValue);
		public override bool Equals(object? obj) => Equals(obj as LIntLiteralValue);
		public override int GetHashCode() => Value.GetHashCode();
		public override string ToString() => $"LINT#{Value}";
	}

	public sealed class BooleanLiteralValue : ILiteralValue, IEquatable<BooleanLiteralValue>
	{
		public readonly bool Value;
		public BooleanLiteralValue(bool value, IType type)
		{
			Value = value;
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public IType Type { get; }
		public bool Equals(BooleanLiteralValue? other) => other != null && other.Value == Value;
		public bool Equals(ILiteralValue? other) => Equals(other as BooleanLiteralValue);
		public override bool Equals(object? obj) => Equals(obj as BooleanLiteralValue);
		public override int GetHashCode() => Value.GetHashCode();
		public override string ToString() => Value ? "TRUE" : "FALSE";
	}
	public sealed class UnknownLiteralValue : ILiteralValue, IEquatable<UnknownLiteralValue>
	{
		public UnknownLiteralValue(IType type)
		{
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public IType Type { get; }

		public bool Equals(UnknownLiteralValue? other) => other != null;
		public bool Equals(ILiteralValue? other) => Equals(other as UnknownLiteralValue);
		public override bool Equals(object? obj) => Equals(obj as UnknownLiteralValue);
		public override int GetHashCode() => 0;
		public override string ToString() => "Unknown";
	}
	public sealed class EnumLiteralValue : ILiteralValue, IEquatable<EnumLiteralValue>
	{
		public readonly ILiteralValue InnerValue;
		public EnumLiteralValue(EnumTypeSymbol type, ILiteralValue innerValue)
		{
			Type = type ?? throw new ArgumentNullException(nameof(type));
			InnerValue = innerValue ?? throw new ArgumentNullException(nameof(innerValue));
		}

		public EnumTypeSymbol Type { get; }
		IType ILiteralValue.Type => Type;

		public bool Equals(EnumLiteralValue? other) => other != null && TypeRelations.IsIdentical(Type, other.Type) && InnerValue.Equals(other.InnerValue);
		public bool Equals(ILiteralValue? other) => Equals(other as EnumLiteralValue);
		public override bool Equals(object? obj) => Equals(obj as EnumLiteralValue);
		public override int GetHashCode() => HashCode.Combine(Type, InnerValue);
		public override string ToString() => $"{Type}({InnerValue})";
	}

	public sealed class NullPointerLiteralValue : ILiteralValue, IEquatable<NullPointerLiteralValue>
	{
		public readonly PointerType Type;

		public NullPointerLiteralValue(PointerType type)
		{
			Type = type;
		}

		IType ILiteralValue.Type => Type;

		public bool Equals(ILiteralValue? other) => Equals(other as NullPointerLiteralValue);
		public bool Equals(NullPointerLiteralValue? other) => other != null && TypeRelations.IsIdentical(Type, other.Type);
		public override bool Equals(object? obj) => throw new NotImplementedException();
		public override int GetHashCode() => 0;
		public override string ToString() => "0";
	}
}
