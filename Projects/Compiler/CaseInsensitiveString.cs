using System;

namespace Compiler
{
	public struct CaseInsensitiveString : IEquatable<CaseInsensitiveString>
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

		public static bool operator ==(CaseInsensitiveString left, CaseInsensitiveString right) => left.Equals(right);
		public static bool operator !=(CaseInsensitiveString left, CaseInsensitiveString right) => !(left == right);
	}

	public static class CaseInsensitiveStringExt
	{
		public static CaseInsensitiveString ToCaseInsensitive(this string self) => new (self);
	}
}
