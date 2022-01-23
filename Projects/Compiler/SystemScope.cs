using System;
using Compiler.Types;

namespace Compiler
{
	public sealed class SystemScope
	{
		public BuiltInType LReal => s_BuiltInTypeTable.LReal;
		public BuiltInType Real => s_BuiltInTypeTable.Real;
		public BuiltInType LInt => s_BuiltInTypeTable.LInt;
		public BuiltInType DInt => s_BuiltInTypeTable.DInt;
		public BuiltInType Int => s_BuiltInTypeTable.Int;
		public BuiltInType SInt => s_BuiltInTypeTable.SInt;
		public BuiltInType ULInt => s_BuiltInTypeTable.ULInt;
		public BuiltInType UDInt => s_BuiltInTypeTable.UDInt;
		public BuiltInType UInt => s_BuiltInTypeTable.UInt;
		public BuiltInType USInt => s_BuiltInTypeTable.USInt;
		public BuiltInType Bool => s_BuiltInTypeTable.Bool;
		public BuiltInType LTime => s_BuiltInTypeTable.LTime;
		public BuiltInType Time => s_BuiltInTypeTable.Time;

		private static readonly BuiltInTypeTable s_BuiltInTypeTable = new ();
		private static readonly BuiltInFunctionTable s_BuiltInFunctionTable = new (s_BuiltInTypeTable);
		public BuiltInTypeTable BuiltInTypeTable = s_BuiltInTypeTable;
		public BuiltInFunctionTable BuiltInFunctionTable = s_BuiltInFunctionTable;

		public SystemScope(CaseInsensitiveString moduleName)
		{
			PointerSize = 4;
			ModuleName = moduleName;
		}

		public CaseInsensitiveString ModuleName { get; }

		public ILiteralValue? TryCreateLiteralFromRealValue(OverflowingReal value, IType context)
			=> s_BuiltInTypeTable.TryCreateLiteralFromRealValue(value, context);
		public ILiteralValue? TryCreateLiteralFromIntValue(OverflowingInteger value, IType context)
			=> s_BuiltInTypeTable.TryCreateLiteralFromIntValue(value, context);
		public ILiteralValue? TryCreateLiteralFromDurationValue(OverflowingDuration value, IType context)
			=> s_BuiltInTypeTable.TryCreateLiteralFromDurationValue(value, context);
		public ILiteralValue? TryCreateIntLiteral(OverflowingInteger value)
			=> s_BuiltInTypeTable.TryCreateIntLiteral(value);
		public IType? GetSignedIntegerTypeGreaterThan(int size)
			=> GetSignedIntegerTypeGreaterEqualThan(size + 1);
		public IType? GetSignedIntegerTypeGreaterEqualThan(int size)
			=> s_BuiltInTypeTable.GetSignedIntegerTypeGreaterEqualThan(size);
		
		public IType PointerDiffrenceType => GetSignedIntegerTypeGreaterEqualThan(PointerSize)!;
		public int PointerSize { get; }

		public IType? GetSmallestCommonImplicitCastType(IType a, IType b)
		{
			if (b is null)
				throw new ArgumentNullException(nameof(b));
			if (a is null)
				throw new ArgumentNullException(nameof(a));

			// SAME + SAME = SAME
			// Enum + Anyting => BaseType(Enum) + Anything
			// LREAL + Anything => LREAL
			// REAL + Anything => REAL
			// Signed + Signed => The bigger one
			// Unsigned + Unsigned => The bigger one
			// Signed + Unsigned => The next signed type that is bigger than both.
			if (TypeRelations.IsIdentical(a, b))
				return a;
			if (TypeRelations.IsAliasType(a, out var aliasTypeSymbolA))
				return GetSmallestCommonImplicitCastType(aliasTypeSymbolA.AliasedType, b);
			if (TypeRelations.IsAliasType(b, out var aliasTypeSymbolB))
				return GetSmallestCommonImplicitCastType(a, aliasTypeSymbolB.AliasedType);
			if (TypeRelations.IsEnumType(a, out var enumA))
				return GetSmallestCommonImplicitCastType(enumA.BaseType, b);
			if (TypeRelations.IsEnumType(b, out var enumB))
				return GetSmallestCommonImplicitCastType(a, enumB.BaseType);

			if (TypeRelations.IsBuiltInType(a, out var builtInA) && TypeRelations.IsBuiltInType(b, out var builtInB) && builtInA.IsArithmetic && builtInB.IsArithmetic)
			{
				// Must be compatible with IsAllowedArithmeticImplicitCast
				if (builtInA.Equals(LReal) || builtInB.Equals(LReal))
					return LReal;
				if (builtInA.Equals(Real) || builtInB.Equals(Real))
					return Real;
				if ((builtInA.IsUnsignedInt && builtInB.IsUnsignedInt) || (builtInA.IsSignedInt && builtInB.IsSignedInt))
					return TypeRelations.MaxSizeType(builtInA, builtInB);
				return GetSignedIntegerTypeGreaterThan(Math.Max(builtInA.Size, builtInB.Size));
			}
			return null;
		}
	}
}
