using Superpower;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Runtime.IR.RuntimeTypes
{
    public readonly struct ArrayTypeRange : IEquatable<ArrayTypeRange>
    {
        public readonly int LowerBound;
        public readonly int UpperBound;

        public ArrayTypeRange(int lowerBound, int upperBound)
        {
            if (upperBound - lowerBound + 1 < 0)
                throw new ArgumentException($"Upperbound({upperBound}) must be in range [lowerBound({lowerBound})-1, inf].", nameof(upperBound));
            LowerBound = lowerBound;
            UpperBound = upperBound;
        }

        public int Size => UpperBound - LowerBound + 1;
        public bool IsInRange(int index) => index >= LowerBound && index <= UpperBound;

        public bool Equals(ArrayTypeRange other) => LowerBound == other.LowerBound && UpperBound == other.UpperBound;
        public override bool Equals(object? obj) => throw new NotImplementedException("Use Equals(ArrayRange) instead");
        public override int GetHashCode() => HashCode.Combine(LowerBound, UpperBound);
        public override string ToString() => $"{LowerBound}..{UpperBound}";
        public static bool operator ==(ArrayTypeRange left, ArrayTypeRange right) => left.Equals(right);
        public static bool operator !=(ArrayTypeRange left, ArrayTypeRange right) => !(left == right);

        public IEnumerable<int> Values => Enumerable.Range(LowerBound, Size);

        public static readonly TextParser<ArrayTypeRange> Parser =
            from start in Superpower.Parsers.Numerics.IntegerInt32
            from _2 in Superpower.Parsers.Span.EqualTo("..").SuroundOptionalWhitespace()
            from end in Superpower.Parsers.Numerics.IntegerInt32
            select new ArrayTypeRange(start, end);
    }
}
