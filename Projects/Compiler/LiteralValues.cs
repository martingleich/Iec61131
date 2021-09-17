using Compiler.Types;
using System;

namespace Compiler
{
	public interface ILiteralValue : IEquatable<ILiteralValue>
	{
		public IType Type { get; }
	}
	public sealed class DIntLiteralValue : ILiteralValue, IEquatable<DIntLiteralValue>
	{
		public readonly int Value;

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

		public bool Equals(EnumLiteralValue? other) => other != null && TypeRelations.IsIdenticalType(Type, other.Type) && InnerValue.Equals(other.InnerValue);
		public bool Equals(ILiteralValue? other) => Equals(other as EnumLiteralValue);
		public override bool Equals(object? obj) => Equals(obj as EnumLiteralValue);
		public override int GetHashCode() => HashCode.Combine(Type, InnerValue);
		public override string ToString() => $"{Type}({InnerValue})";
	}
}
