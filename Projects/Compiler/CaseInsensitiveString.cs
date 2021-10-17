using System;

namespace Compiler
{

	public readonly struct CaseInsensitiveString : IEquatable<CaseInsensitiveString>, IComparable<CaseInsensitiveString>
	{
		private readonly string Value;
		public string Original => Value; 

		public CaseInsensitiveString(string value)
		{
			Value = value ?? throw new ArgumentNullException(nameof(value));
		}

		public bool Equals(CaseInsensitiveString other) => other.Value.Equals(Value, StringComparison.InvariantCultureIgnoreCase);
		public override bool Equals(object? obj) => obj is CaseInsensitiveString cis && Equals(cis);
		public override int GetHashCode() => Value.GetHashCode(StringComparison.InvariantCultureIgnoreCase);

		public override string ToString() => Value;

		public int CompareTo(CaseInsensitiveString other) => string.Compare(Value, other.Value, StringComparison.InvariantCultureIgnoreCase);
		public static bool operator ==(CaseInsensitiveString left, CaseInsensitiveString right) => left.Equals(right);
		public static bool operator !=(CaseInsensitiveString left, CaseInsensitiveString right) => !(left == right);
		public static bool operator <(CaseInsensitiveString left, CaseInsensitiveString right) => left.CompareTo(right) < 0;
		public static bool operator <=(CaseInsensitiveString left, CaseInsensitiveString right) => left.CompareTo(right) <= 0;
		public static bool operator >(CaseInsensitiveString left, CaseInsensitiveString right) => left.CompareTo(right) > 0;
		public static bool operator >=(CaseInsensitiveString left, CaseInsensitiveString right) => left.CompareTo(right) >= 0;
	}

	public static class CaseInsensitiveStringExt
	{
		public static CaseInsensitiveString ToCaseInsensitive(this string self) => new (self);
		/// <summary>
		/// Prevents people from calling ToCaseInsensitive on an already converted string.
		/// </summary>
		/// <param name="_"></param>
		public static void ToCaseInsensitive(this CaseInsensitiveString _) { }
	}
}
