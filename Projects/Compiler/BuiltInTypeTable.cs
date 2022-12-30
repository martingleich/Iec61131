﻿using Compiler.Types;
using Runtime.IR.RuntimeTypes;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Compiler
{
	public sealed class BuiltInTypeTable
	{
		public readonly ImmutableArray<BuiltInType> AllBuiltInTypes;
		public readonly ImmutableArray<BuiltInType> ArithmeticTypes;
		public readonly ImmutableArray<BuiltInType> IntegerTypes;
		public readonly ImmutableArray<BuiltInType> RealTypes;
		public readonly BuiltInType Char = new(1, 1, "CHAR");
		public readonly BuiltInType LReal = new(RuntimeTypeLREAL.Instance, 8, BuiltInType.Flag.Real);
		public readonly BuiltInType Real = new(RuntimeTypeREAL.Instance, 4, BuiltInType.Flag.Real);
		public readonly BuiltInType LInt = new(RuntimeTypeLINT.Instance, 8, BuiltInType.Flag.SInt);
		public readonly BuiltInType DInt = new(RuntimeTypeDINT.Instance, 4, BuiltInType.Flag.SInt);
		public readonly BuiltInType Int = new(RuntimeTypeINT.Instance, 2, BuiltInType.Flag.SInt);
		public readonly BuiltInType SInt = new(RuntimeTypeSINT.Instance, 1, BuiltInType.Flag.SInt);
		public readonly BuiltInType ULInt = new(8, 8, "ULINT", BuiltInType.Flag.UInt);
		public readonly BuiltInType UDInt = new(4, 4, "UDINT", BuiltInType.Flag.UInt);
		public readonly BuiltInType UInt = new(2, 2, "UINT", BuiltInType.Flag.UInt);
		public readonly BuiltInType USInt = new(1, 1, "USINT", BuiltInType.Flag.UInt);
		public readonly BuiltInType LWord = new(8, 8, "LWORD");
		public readonly BuiltInType DWord = new(4, 4, "DWORD");
		public readonly BuiltInType Word = new(2, 2, "WORD");
		public readonly BuiltInType Byte = new(1, 1, "BYTE");
		public readonly BuiltInType Bool = new(RuntimeTypeBOOL.Instance, 1);
		public readonly BuiltInType LTime = new(8, 8, "LTIME");
		public readonly BuiltInType Time = new(4, 4, "TIME");
		public readonly BuiltInType LDT = new(8, 8, "LDT");
		public readonly BuiltInType DT = new(4, 4, "DT");
		public readonly BuiltInType LDate = new(8, 8, "LDATE");
		public readonly BuiltInType Date = new(4, 4, "DATE");
		public readonly BuiltInType LTOD = new(8, 8, "LTOD");
		public readonly BuiltInType TOD = new(4, 4, "TOD");

		private readonly TypeMapper BuiltInTypeMapper;
		public BuiltInTypeTable()
		{
			AllBuiltInTypes = ImmutableArray.Create(Char, LReal, Real, LInt, DInt, Int, SInt, ULInt, UDInt, UInt, USInt, LWord, DWord, Word, Byte, Bool, LTime, Time, LDT, DT, LDate, Date, LTOD, TOD);
			ArithmeticTypes = AllBuiltInTypes.Where(t => t.IsArithmetic).ToImmutableArray();
			IntegerTypes = ImmutableArray.Create(LInt, DInt, Int, SInt, ULInt, UDInt, UInt, USInt);
			RealTypes = ImmutableArray.Create(LReal, Real);
			BuiltInTypeMapper = new TypeMapper(this);
		}
		public BuiltInType MapTokenToType(IBuiltInTypeToken token) => token.Accept(BuiltInTypeMapper);

		public ILiteralValue? TryCreateLiteralFromDurationValue(OverflowingDuration value, IType targetType)
		{
			if (TypeRelations.IsIdentical(targetType, LTime))
			{
				if (value.TryGetDurationNs64(out var x))
					return new LTimeLiteralValue(x, targetType);
			}
			else if (TypeRelations.IsIdentical(targetType, Time))
			{
				if (value.TryGetDurationMs32(out var x))
					return new TimeLiteralValue(x, targetType);
			}
			return null;
		}
		public ILiteralValue? TryCreateLiteralFromRealValue(OverflowingReal value, IType targetType)
		{
			if (TypeRelations.IsIdentical(targetType, Real))
			{
				if (value.TryGetSingle(out var x))
					return new RealLiteralValue(x, targetType);
			}
			else if (TypeRelations.IsIdentical(targetType, LReal))
			{
				if (value.TryGetDouble(out var x))
					return new LRealLiteralValue(x, targetType);
			}
			return null;
		}
		public ILiteralValue? TryCreateLiteralFromIntValue(OverflowingInteger value, IType targetType)
		{
			if (TypeRelations.IsIdentical(targetType, Bool))
			{
				if (value.IsZero)
					return new BooleanLiteralValue(false, Bool);
				else if (value.IsOne)
					return new BooleanLiteralValue(true, Bool);
			}
			else if (TypeRelations.IsIdentical(targetType, Real))
			{
				if (value.TryGetSingle(out var x))
					return new RealLiteralValue(x, Real);
			}
			else if (TypeRelations.IsIdentical(targetType, LReal))
			{
				if (value.TryGetDouble(out var x))
					return new LRealLiteralValue(x, LReal);
			}
			if (TypeRelations.IsIdentical(targetType, SInt))
			{
				if (value.TryGetSByte(out var x))
					return new SIntLiteralValue(x, targetType);
			}
			else if (TypeRelations.IsIdentical(targetType, USInt))
			{
				if (value.TryGetByte(out var x))
					return new USIntLiteralValue(x, targetType);
			}
			else if (TypeRelations.IsIdentical(targetType, Int))
			{
				if (value.TryGetShort(out var x))
					return new IntLiteralValue(x, targetType);
			}
			else if (TypeRelations.IsIdentical(targetType, UInt))
			{
				if (value.TryGetUShort(out var x))
					return new UIntLiteralValue(x, targetType);
			}
			else if (TypeRelations.IsIdentical(targetType, DInt))
			{
				if (value.TryGetInt(out var x))
					return new DIntLiteralValue(x, targetType);
			}
			else if (TypeRelations.IsIdentical(targetType, UDInt))
			{
				if (value.TryGetUInt(out var x))
					return new UDIntLiteralValue(x, targetType);
			}
			else if (TypeRelations.IsIdentical(targetType, LInt))
			{
				if (value.TryGetLong(out var x))
					return new LIntLiteralValue(x, targetType);
			}
			else if (TypeRelations.IsIdentical(targetType, ULInt))
			{
				if (value.TryGetULong(out var x))
					return new ULIntLiteralValue(x, targetType);
			}
			return null;
		}
		public ILiteralValue? TryCreateIntLiteral(OverflowingInteger value)
		{
			if (value.TryGetShort(out short shortValue))
				return new IntLiteralValue(shortValue, Int);
			else if (value.TryGetUShort(out ushort ushortValue))
				return new UIntLiteralValue(ushortValue, UInt);
			else if (value.TryGetInt(out int intValue))
				return new DIntLiteralValue(intValue, DInt);
			else if (value.TryGetUInt(out uint uintValue))
				return new UDIntLiteralValue(uintValue, UDInt);
			else if (value.TryGetLong(out long longValue))
				return new LIntLiteralValue(longValue, LInt);
			else if (value.TryGetULong(out ulong ulongValue))
				return new ULIntLiteralValue(ulongValue, ULInt);
			else
				return null;
		}
		public IType? GetSignedIntegerTypeGreaterEqualThan(int size)
		{
			if (SInt.Size >= size)
				return SInt;
			else if (Int.Size >= size)
				return Int;
			else if (DInt.Size >= size)
				return DInt;
			else if (LInt.Size >= size)
				return LInt;
			else
				return null;
		}
		public Func<IType, ILiteralValue[], ILiteralValue>? GetCasterFunction(BuiltInType from, BuiltInType to)
		{
			if (from.Equals(to))
				return (_, v) => v[0];
            if (from.IsInt)
                return (result, args) => ArithmeticCast_FromInt((IAnyIntLiteralValue)args[0], result, this);
            else if (from.IsReal && from.Size==4 && to.IsReal && to.Size == 8)
                return Real_To_LReal;
            else
                return null;
            static ILiteralValue ArithmeticCast_FromInt(IAnyIntLiteralValue intLiteralValue, IType targetType, BuiltInTypeTable builtInTypeTable)
            {
                var resultValue = builtInTypeTable.TryCreateLiteralFromIntValue(intLiteralValue.Value, targetType);
                return resultValue ?? throw new InvalidOperationException("Implicit arithmetic cast must always succeed");
            }
            static ILiteralValue Real_To_LReal(IType result, ILiteralValue[] args)
                => new LRealLiteralValue(((RealLiteralValue)args[0]).Value, result);

		}
		public bool CanImplicitCast(BuiltInType from, BuiltInType to) => ((from.IsReal && to.IsReal) || (from.IsSignedInt && to.IsSignedInt) || (from.IsUnsignedInt && to.IsUnsignedInt)) && from.Size <= to.Size;
		public Func<IType, ILiteralValue[], ILiteralValue>? GetImplicitCasterFunction(BuiltInType from, BuiltInType to) => CanImplicitCast(from, to) ? GetCasterFunction(from, to) : null;

		private sealed class TypeMapper : IBuiltInTypeToken.IVisitor<BuiltInType>
		{
			private readonly BuiltInTypeTable BuiltInTypeTable;

			public TypeMapper(BuiltInTypeTable builtInTypeTable)
			{
				BuiltInTypeTable = builtInTypeTable ?? throw new ArgumentNullException(nameof(builtInTypeTable));
			}

			public BuiltInType Visit(CharToken charToken) => BuiltInTypeTable.Char;
			public BuiltInType Visit(LRealToken lRealToken) => BuiltInTypeTable.LReal;
			public BuiltInType Visit(RealToken realToken) => BuiltInTypeTable.Real;
			public BuiltInType Visit(LIntToken lIntToken) => BuiltInTypeTable.LInt;
			public BuiltInType Visit(DIntToken dIntToken) => BuiltInTypeTable.DInt;
			public BuiltInType Visit(IntToken intToken) => BuiltInTypeTable.Int;
			public BuiltInType Visit(SIntToken sIntToken) => BuiltInTypeTable.SInt;
			public BuiltInType Visit(ULIntToken uLIntToken) => BuiltInTypeTable.ULInt;
			public BuiltInType Visit(UDIntToken uDIntToken) => BuiltInTypeTable.UDInt;
			public BuiltInType Visit(UIntToken uIntToken) => BuiltInTypeTable.UInt;
			public BuiltInType Visit(USIntToken uSIntToken) => BuiltInTypeTable.USInt;
			public BuiltInType Visit(LWordToken lWordToken) => BuiltInTypeTable.LWord;
			public BuiltInType Visit(DWordToken dWordToken) => BuiltInTypeTable.DWord;
			public BuiltInType Visit(WordToken wordToken) => BuiltInTypeTable.Word;
			public BuiltInType Visit(ByteToken byteToken) => BuiltInTypeTable.Byte;
			public BuiltInType Visit(BoolToken boolToken) => BuiltInTypeTable.Bool;
			public BuiltInType Visit(LTimeToken lTimeToken) => BuiltInTypeTable.LTime;
			public BuiltInType Visit(TimeToken timeToken) => BuiltInTypeTable.Time;
			public BuiltInType Visit(LDTToken lDTToken) => BuiltInTypeTable.LDT;
			public BuiltInType Visit(DTToken dTToken) => BuiltInTypeTable.DT;
			public BuiltInType Visit(LDateToken lDateToken) => BuiltInTypeTable.LDate;
			public BuiltInType Visit(DateToken dateToken) => BuiltInTypeTable.Date;
			public BuiltInType Visit(LTODToken lTODToken) => BuiltInTypeTable.LTOD;
			public BuiltInType Visit(TODToken tODToken) => BuiltInTypeTable.TOD;
		}
	}
}
