﻿using Compiler.Types;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Compiler
{
	public sealed class BuiltInFunctionTable
	{
		private readonly ImmutableDictionary<FunctionVariableSymbol, Func<IType, ILiteralValue[], ILiteralValue>?> Table;

		private static readonly CaseInsensitiveString SystemModuleName = "__System".ToCaseInsensitive();

		private static FunctionVariableSymbol BinaryOperator(string baseName, BuiltInType type)
			=> BinaryOperator(baseName, type, type);
		private static FunctionVariableSymbol BinaryOperator(string baseName, BuiltInType type, BuiltInType returnType)
		{
			var name = OperatorName(baseName, type);
			var funcType = new FunctionTypeSymbol(SystemModuleName, name, default, OrderedSymbolSet.ToOrderedSymbolSet<ParameterVariableSymbol>(
				new(ParameterKind.Input, default, "LEFT".ToCaseInsensitive(), type),
				new(ParameterKind.Input, default, "RIGHT".ToCaseInsensitive(), type),
				new(ParameterKind.Output, default, name, returnType)));
			return new FunctionVariableSymbol(funcType);
		}
		private static FunctionVariableSymbol UnaryOperator(string baseName, BuiltInType type)
		{
			var name = OperatorName(baseName, type);
			var funcType = new FunctionTypeSymbol(SystemModuleName, name, default, OrderedSymbolSet.ToOrderedSymbolSet<ParameterVariableSymbol>(
				new(ParameterKind.Input, default, "VALUE".ToCaseInsensitive(), type),
				new(ParameterKind.Output, default, name, type)));
			return new FunctionVariableSymbol(funcType);
		}

		private static CaseInsensitiveString OperatorName(string baseName, BuiltInType type)
			=> (baseName + "_" + type.Name).ToCaseInsensitive();

		private static FunctionVariableSymbol CastOperator(BuiltInType from, BuiltInType to)
		{
			var name = CastFunctionName(from, to);
			var funcType = new FunctionTypeSymbol(SystemModuleName, name, default, OrderedSymbolSet.ToOrderedSymbolSet<ParameterVariableSymbol>(
				new(ParameterKind.Input, default, "VALUE".ToCaseInsensitive(), from),
				new(ParameterKind.Output, default, name, to)));
			return new FunctionVariableSymbol(funcType);
		}

		private static CaseInsensitiveString CastFunctionName(BuiltInType from, BuiltInType to)
			=> $"{from.Name}_TO_{to.Name}".ToCaseInsensitive();

		public BuiltInFunctionTable(SystemScope systemScope)
		{
			var builder = ImmutableDictionary.CreateBuilder<FunctionVariableSymbol, Func<IType, ILiteralValue[], ILiteralValue>?>(SymbolByNameComparer<FunctionVariableSymbol>.Instance);

			builder.Add(BinaryOperator("ADD", systemScope.SInt), AddSINT);
			builder.Add(BinaryOperator("SUB", systemScope.SInt), SubSINT);
			builder.Add(BinaryOperator("MUL", systemScope.SInt), MulSINT);
			builder.Add(BinaryOperator("DIV", systemScope.SInt), DivSINT);
			builder.Add(BinaryOperator("MOD", systemScope.SInt), ModSINT);
			builder.Add(UnaryOperator("NEG", systemScope.SInt), NegSINT);

			builder.Add(BinaryOperator("ADD", systemScope.USInt), AddUSINT);
			builder.Add(BinaryOperator("SUB", systemScope.USInt), SubUSINT);
			builder.Add(BinaryOperator("MUL", systemScope.USInt), MulUSINT);
			builder.Add(BinaryOperator("DIV", systemScope.USInt), DivUSINT);
			builder.Add(BinaryOperator("MOD", systemScope.USInt), ModUSINT);

			builder.Add(BinaryOperator("ADD", systemScope.Int), AddINT);
			builder.Add(BinaryOperator("SUB", systemScope.Int), SubINT);
			builder.Add(BinaryOperator("MUL", systemScope.Int), MulINT);
			builder.Add(BinaryOperator("DIV", systemScope.Int), DivINT);
			builder.Add(BinaryOperator("MOD", systemScope.Int), ModINT);
			builder.Add(UnaryOperator("NEG", systemScope.Int), NegINT);

			builder.Add(BinaryOperator("ADD", systemScope.UInt), AddUINT);
			builder.Add(BinaryOperator("SUB", systemScope.UInt), SubUINT);
			builder.Add(BinaryOperator("MUL", systemScope.UInt), MulUINT);
			builder.Add(BinaryOperator("DIV", systemScope.UInt), DivUINT);
			builder.Add(BinaryOperator("MOD", systemScope.UInt), ModUINT);

			builder.Add(BinaryOperator("ADD", systemScope.DInt), AddDINT);
			builder.Add(BinaryOperator("SUB", systemScope.DInt), SubDINT);
			builder.Add(BinaryOperator("MUL", systemScope.DInt), MulDINT);
			builder.Add(BinaryOperator("DIV", systemScope.DInt), DivDINT);
			builder.Add(BinaryOperator("MOD", systemScope.DInt), ModDINT);
			builder.Add(UnaryOperator("NEG", systemScope.DInt), NegDINT);

			builder.Add(BinaryOperator("ADD", systemScope.UDInt), AddUDINT);
			builder.Add(BinaryOperator("SUB", systemScope.UDInt), SubUDINT);
			builder.Add(BinaryOperator("MUL", systemScope.UDInt), MulUDINT);
			builder.Add(BinaryOperator("DIV", systemScope.UDInt), DivUDINT);
			builder.Add(BinaryOperator("MOD", systemScope.UDInt), ModUDINT);

			builder.Add(BinaryOperator("ADD", systemScope.LInt), AddLINT);
			builder.Add(BinaryOperator("SUB", systemScope.LInt), SubLINT);
			builder.Add(BinaryOperator("MUL", systemScope.LInt), MulLINT);
			builder.Add(BinaryOperator("DIV", systemScope.LInt), DivLINT);
			builder.Add(BinaryOperator("MOD", systemScope.LInt), ModLINT);
			builder.Add(UnaryOperator("NEG", systemScope.LInt), NegLINT);

			builder.Add(BinaryOperator("MOD", systemScope.ULInt), ModULINT);
			builder.Add(BinaryOperator("ADD", systemScope.ULInt), AddULINT);
			builder.Add(BinaryOperator("SUB", systemScope.ULInt), SubULINT);
			builder.Add(BinaryOperator("MUL", systemScope.ULInt), MulULINT);
			builder.Add(BinaryOperator("DIV", systemScope.ULInt), DivULINT);
			builder.Add(BinaryOperator("MOD", systemScope.ULInt), ModULINT);

			builder.Add(BinaryOperator("ADD", systemScope.LTime), AddLTIME);
			builder.Add(BinaryOperator("SUB", systemScope.LTime), SubLTIME);
			builder.Add(BinaryOperator("NEG", systemScope.LTime), NegLTIME);
			builder.Add(BinaryOperator("MOD", systemScope.LTime), ModLTIME);

			builder.Add(BinaryOperator("ADD", systemScope.Time), AddTIME);
			builder.Add(BinaryOperator("SUB", systemScope.Time), SubTIME);
			builder.Add(BinaryOperator("NEG", systemScope.Time), NegTIME);
			builder.Add(BinaryOperator("MOD", systemScope.Time), ModTIME);

			builder.Add(BinaryOperator("ADD", systemScope.Real), null);
			builder.Add(BinaryOperator("SUB", systemScope.Real), null);
			builder.Add(BinaryOperator("MUL", systemScope.Real), null);
			builder.Add(BinaryOperator("DIV", systemScope.Real), null);
			builder.Add(BinaryOperator("MOD", systemScope.Real), null);
			builder.Add(BinaryOperator("NEG", systemScope.Real), null);

			builder.Add(BinaryOperator("ADD", systemScope.LReal), null);
			builder.Add(BinaryOperator("SUB", systemScope.LReal), null);
			builder.Add(BinaryOperator("MUL", systemScope.LReal), null);
			builder.Add(BinaryOperator("DIV", systemScope.LReal), null);
			builder.Add(BinaryOperator("MOD", systemScope.LReal), null);
			builder.Add(BinaryOperator("NEG", systemScope.LReal), null);

			AddComparisons(builder, systemScope.SInt, systemScope.Bool, (x, y) => ((SIntLiteralValue)x).Value <= ((SIntLiteralValue)y).Value);
			AddComparisons(builder, systemScope.USInt, systemScope.Bool, (x, y) => ((USIntLiteralValue)x).Value <= ((USIntLiteralValue)y).Value);
			AddComparisons(builder, systemScope.Int, systemScope.Bool, (x, y) => ((IntLiteralValue)x).Value <= ((IntLiteralValue)y).Value);
			AddComparisons(builder, systemScope.UInt, systemScope.Bool, (x, y) => ((UIntLiteralValue)x).Value <= ((UIntLiteralValue)y).Value);
			AddComparisons(builder, systemScope.DInt, systemScope.Bool, (x, y) => ((DIntLiteralValue)x).Value <= ((DIntLiteralValue)y).Value);
			AddComparisons(builder, systemScope.UDInt, systemScope.Bool, (x, y) => ((UDIntLiteralValue)x).Value <= ((UDIntLiteralValue)y).Value);
			AddComparisons(builder, systemScope.LInt, systemScope.Bool, (x, y) => ((LIntLiteralValue)x).Value <= ((LIntLiteralValue)y).Value);
			AddComparisons(builder, systemScope.ULInt, systemScope.Bool, (x, y) => ((ULIntLiteralValue)x).Value <= ((ULIntLiteralValue)y).Value);
			AddComparisons(builder, systemScope.Time, systemScope.Bool, (x, y) => ((TimeLiteralValue)x).Value <= ((TimeLiteralValue)y).Value);
			AddComparisons(builder, systemScope.LTime, systemScope.Bool, (x, y) => ((LTimeLiteralValue)x).Value <= ((LTimeLiteralValue)y).Value);
			AddComparisons(builder, systemScope.Real, systemScope.Bool);
			AddComparisons(builder, systemScope.LReal, systemScope.Bool);

			AddEquality(builder, systemScope.Bool, systemScope.Bool, (x, y) => ((BooleanLiteralValue)x).Value == ((BooleanLiteralValue)y).Value);
			builder.Add(BinaryOperator("AND", systemScope.Bool), AndBool);
			builder.Add(BinaryOperator("OR", systemScope.Bool), OrBool);
			builder.Add(BinaryOperator("XOR", systemScope.Bool), XorBool);
			builder.Add(UnaryOperator("NOT", systemScope.Bool), NotBool);

			foreach (var fromType in systemScope.AllBuiltInTypes)
			{
				foreach (var toType in systemScope.AllBuiltInTypes)
				{
					if (!fromType.Equals(toType) && IsAllowedArithmeticImplicitCast(fromType, toType))
					{
						Func<IType, ILiteralValue[], ILiteralValue>? func;
						if (fromType.IsInt)
							func = (result, args) => ArithmeticCast_FromInt((IAnyIntLiteralValue)args[0], result, systemScope);
						else if (TypeRelations.IsIdentical(fromType, systemScope.Real) && TypeRelations.IsIdentical(toType, systemScope.LReal))
							func = Real_To_LReal;
						else
							func = null;
						builder.Add(CastOperator(fromType, toType), func);
					}
				}
			}

			Table = builder.ToImmutable();
			AllFunctions = Table.Keys.ToSymbolSet();
		}
		private static bool IsAllowedArithmeticImplicitCast(BuiltInType builtInSource, BuiltInType builtInTarget)
		{
			// Okay casts:
			// int+real TO LREAL
			// int TO REAL
			// unsigned int TO larger (unsigned|signed) int
			// signed int TO larger signed int
			if (builtInSource is null)
				throw new ArgumentNullException(nameof(builtInSource));
			if (builtInTarget is null)
				throw new ArgumentNullException(nameof(builtInTarget));
			if (!builtInSource.IsArithmetic || !builtInTarget.IsArithmetic)
				return false;
			return ((builtInSource.IsInt || (builtInSource.IsReal && builtInSource.Size <= builtInTarget.Size)) && builtInTarget.IsReal)
				|| (builtInSource.IsUnsignedInt && builtInTarget.IsInt && builtInSource.Size <= builtInTarget.Size)
				|| (builtInSource.IsSignedInt && builtInTarget.IsSignedInt && builtInSource.Size <= builtInTarget.Size);
		}

		private static void AddComparisons(
			ImmutableDictionary<FunctionVariableSymbol, Func<IType, ILiteralValue[], ILiteralValue>?>.Builder builder,
			BuiltInType type,
			BuiltInType boolean,
			Func<ILiteralValue, ILiteralValue, bool> lessEqual)
		{
			ILiteralValue Equal(IType result, ILiteralValue[] args) => new BooleanLiteralValue(lessEqual(args[0], args[1]) & lessEqual(args[1], args[0]), result);
			ILiteralValue NotEqual(IType result, ILiteralValue[] args) => new BooleanLiteralValue(!lessEqual(args[0], args[1]) | !lessEqual(args[1], args[0]), result);
			ILiteralValue LessEqual(IType result, ILiteralValue[] args) => new BooleanLiteralValue(lessEqual(args[0], args[1]), result);
			ILiteralValue Less(IType result, ILiteralValue[] args) => new BooleanLiteralValue(!lessEqual(args[1], args[0]), result);
			ILiteralValue Greater(IType result, ILiteralValue[] args) => new BooleanLiteralValue(!lessEqual(args[0], args[1]), result);
			ILiteralValue GreaterEqual(IType result, ILiteralValue[] args) => new BooleanLiteralValue(lessEqual(args[1], args[0]), result);

			builder.Add(BinaryOperator("EQUAL", type, boolean), Equal);
			builder.Add(BinaryOperator("NOT_EQUAL", type, boolean), NotEqual);
			builder.Add(BinaryOperator("LESS", type, boolean), Less);
			builder.Add(BinaryOperator("LESS_EQUAL", type, boolean), LessEqual);
			builder.Add(BinaryOperator("GREATER", type, boolean), Greater);
			builder.Add(BinaryOperator("GREATER_EQUAL", type, boolean), GreaterEqual);
		}

		private static void AddComparisons(
			ImmutableDictionary<FunctionVariableSymbol, Func<IType, ILiteralValue[], ILiteralValue>?>.Builder builder,
			BuiltInType type,
			BuiltInType boolean)
		{
			builder.Add(BinaryOperator("EQUAL", type, boolean), null);
			builder.Add(BinaryOperator("NOT_EQUAL", type, boolean), null);
			builder.Add(BinaryOperator("LESS", type, boolean), null);
			builder.Add(BinaryOperator("LESS_EQUAL", type, boolean), null);
			builder.Add(BinaryOperator("GREATER", type, boolean), null);
			builder.Add(BinaryOperator("GREATER_EQUAL", type, boolean), null);
		}

		private static void AddEquality(
			ImmutableDictionary<FunctionVariableSymbol, Func<IType, ILiteralValue[], ILiteralValue>?>.Builder builder,
			BuiltInType type,
			BuiltInType boolean,
			Func<ILiteralValue, ILiteralValue, bool> equal)
		{
			builder.Add(BinaryOperator("EQUAL", type, boolean), (result, args) => new BooleanLiteralValue(equal(args[0], args[1]), result));
			builder.Add(BinaryOperator("NOT_EQUAL", type, boolean), (result, args) => new BooleanLiteralValue(!equal(args[0], args[1]), result));
		}
		private static ILiteralValue AndBool(IType result, ILiteralValue[] args) => new BooleanLiteralValue(((BooleanLiteralValue)args[0]).Value && ((BooleanLiteralValue)args[1]).Value, result);
		private static ILiteralValue OrBool(IType result, ILiteralValue[] args) => new BooleanLiteralValue(((BooleanLiteralValue)args[0]).Value || ((BooleanLiteralValue)args[1]).Value, result);
		private static ILiteralValue XorBool(IType result, ILiteralValue[] args) => new BooleanLiteralValue(((BooleanLiteralValue)args[0]).Value != ((BooleanLiteralValue)args[1]).Value, result);
		private static ILiteralValue NotBool(IType result, ILiteralValue[] args) => new BooleanLiteralValue(!((BooleanLiteralValue)args[0]).Value, result);

		private static ILiteralValue AddSINT(IType result, ILiteralValue[] args) => new SIntLiteralValue(checked((sbyte)(((SIntLiteralValue)args[0]).Value + ((SIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue SubSINT(IType result, ILiteralValue[] args) => new SIntLiteralValue(checked((sbyte)(((SIntLiteralValue)args[0]).Value - ((SIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue MulSINT(IType result, ILiteralValue[] args) => new SIntLiteralValue(checked((sbyte)(((SIntLiteralValue)args[0]).Value * ((SIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue DivSINT(IType result, ILiteralValue[] args) => new SIntLiteralValue(checked((sbyte)(((SIntLiteralValue)args[0]).Value / ((SIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue ModSINT(IType result, ILiteralValue[] args) => new SIntLiteralValue(checked((sbyte)(((SIntLiteralValue)args[0]).Value % ((SIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue NegSINT(IType result, ILiteralValue[] args) => new SIntLiteralValue(checked((sbyte)-(((SIntLiteralValue)args[0]).Value)), result);

		private static ILiteralValue AddUSINT(IType result, ILiteralValue[] args) => new USIntLiteralValue(checked((byte)(((USIntLiteralValue)args[0]).Value + ((USIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue SubUSINT(IType result, ILiteralValue[] args) => new USIntLiteralValue(checked((byte)(((USIntLiteralValue)args[0]).Value - ((USIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue MulUSINT(IType result, ILiteralValue[] args) => new USIntLiteralValue(checked((byte)(((USIntLiteralValue)args[0]).Value * ((USIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue DivUSINT(IType result, ILiteralValue[] args) => new USIntLiteralValue(checked((byte)(((USIntLiteralValue)args[0]).Value / ((USIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue ModUSINT(IType result, ILiteralValue[] args) => new USIntLiteralValue(checked((byte)(((USIntLiteralValue)args[0]).Value % ((USIntLiteralValue)args[1]).Value)), result);

		private static ILiteralValue AddINT(IType result, ILiteralValue[] args) => new IntLiteralValue(checked((short)(((IntLiteralValue)args[0]).Value + ((IntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue SubINT(IType result, ILiteralValue[] args) => new IntLiteralValue(checked((short)(((IntLiteralValue)args[0]).Value - ((IntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue MulINT(IType result, ILiteralValue[] args) => new IntLiteralValue(checked((short)(((IntLiteralValue)args[0]).Value * ((IntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue DivINT(IType result, ILiteralValue[] args) => new IntLiteralValue(checked((short)(((IntLiteralValue)args[0]).Value / ((IntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue ModINT(IType result, ILiteralValue[] args) => new IntLiteralValue(checked((short)(((IntLiteralValue)args[0]).Value % ((IntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue NegINT(IType result, ILiteralValue[] args) => new IntLiteralValue(checked((short)-(((IntLiteralValue)args[0]).Value)), result);

		private static ILiteralValue AddUINT(IType result, ILiteralValue[] args) => new UIntLiteralValue(checked((ushort)(((UIntLiteralValue)args[0]).Value + ((UIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue SubUINT(IType result, ILiteralValue[] args) => new UIntLiteralValue(checked((ushort)(((UIntLiteralValue)args[0]).Value - ((UIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue MulUINT(IType result, ILiteralValue[] args) => new UIntLiteralValue(checked((ushort)(((UIntLiteralValue)args[0]).Value * ((UIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue DivUINT(IType result, ILiteralValue[] args) => new UIntLiteralValue(checked((ushort)(((UIntLiteralValue)args[0]).Value / ((UIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue ModUINT(IType result, ILiteralValue[] args) => new UIntLiteralValue(checked((ushort)(((UIntLiteralValue)args[0]).Value % ((UIntLiteralValue)args[1]).Value)), result);

		private static ILiteralValue AddDINT(IType result, ILiteralValue[] args) => new DIntLiteralValue(checked((int)(((DIntLiteralValue)args[0]).Value + ((DIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue SubDINT(IType result, ILiteralValue[] args) => new DIntLiteralValue(checked((int)(((DIntLiteralValue)args[0]).Value - ((DIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue MulDINT(IType result, ILiteralValue[] args) => new DIntLiteralValue(checked((int)(((DIntLiteralValue)args[0]).Value * ((DIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue DivDINT(IType result, ILiteralValue[] args) => new DIntLiteralValue(checked((int)(((DIntLiteralValue)args[0]).Value / ((DIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue ModDINT(IType result, ILiteralValue[] args) => new DIntLiteralValue(checked((int)(((DIntLiteralValue)args[0]).Value % ((DIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue NegDINT(IType result, ILiteralValue[] args) => new DIntLiteralValue(checked((int)-(((DIntLiteralValue)args[0]).Value)), result);

		private static ILiteralValue AddUDINT(IType result, ILiteralValue[] args) => new UDIntLiteralValue(checked((uint)(((UIntLiteralValue)args[0]).Value + ((UIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue SubUDINT(IType result, ILiteralValue[] args) => new UDIntLiteralValue(checked((uint)(((UIntLiteralValue)args[0]).Value - ((UIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue MulUDINT(IType result, ILiteralValue[] args) => new UDIntLiteralValue(checked((uint)(((UIntLiteralValue)args[0]).Value * ((UIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue DivUDINT(IType result, ILiteralValue[] args) => new UDIntLiteralValue(checked((uint)(((UIntLiteralValue)args[0]).Value / ((UIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue ModUDINT(IType result, ILiteralValue[] args) => new UDIntLiteralValue(checked((uint)(((UIntLiteralValue)args[0]).Value % ((UIntLiteralValue)args[1]).Value)), result);

		private static ILiteralValue AddLINT(IType result, ILiteralValue[] args) => new LIntLiteralValue(checked((long)(((LIntLiteralValue)args[0]).Value + ((LIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue SubLINT(IType result, ILiteralValue[] args) => new LIntLiteralValue(checked((long)(((LIntLiteralValue)args[0]).Value - ((LIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue MulLINT(IType result, ILiteralValue[] args) => new LIntLiteralValue(checked((long)(((LIntLiteralValue)args[0]).Value * ((LIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue DivLINT(IType result, ILiteralValue[] args) => new LIntLiteralValue(checked((long)(((LIntLiteralValue)args[0]).Value / ((LIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue ModLINT(IType result, ILiteralValue[] args) => new LIntLiteralValue(checked((long)(((LIntLiteralValue)args[0]).Value % ((LIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue NegLINT(IType result, ILiteralValue[] args) => new LIntLiteralValue(checked((long)-(((LIntLiteralValue)args[0]).Value)), result);

		private static ILiteralValue AddULINT(IType result, ILiteralValue[] args) => new ULIntLiteralValue(checked((ulong)(((ULIntLiteralValue)args[0]).Value + ((ULIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue SubULINT(IType result, ILiteralValue[] args) => new ULIntLiteralValue(checked((ulong)(((ULIntLiteralValue)args[0]).Value - ((ULIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue MulULINT(IType result, ILiteralValue[] args) => new ULIntLiteralValue(checked((ulong)(((ULIntLiteralValue)args[0]).Value * ((ULIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue DivULINT(IType result, ILiteralValue[] args) => new ULIntLiteralValue(checked((ulong)(((ULIntLiteralValue)args[0]).Value / ((ULIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue ModULINT(IType result, ILiteralValue[] args) => new ULIntLiteralValue(checked((ulong)(((ULIntLiteralValue)args[0]).Value % ((ULIntLiteralValue)args[1]).Value)), result);

		private static ILiteralValue AddLTIME(IType result, ILiteralValue[] args) => new LTimeLiteralValue(((LTimeLiteralValue)args[0]).Value.CheckedAdd(((LTimeLiteralValue)args[1]).Value), result);
		private static ILiteralValue SubLTIME(IType result, ILiteralValue[] args) => new LTimeLiteralValue(((LTimeLiteralValue)args[0]).Value.CheckedSub(((LTimeLiteralValue)args[1]).Value), result);
		private static ILiteralValue NegLTIME(IType result, ILiteralValue[] args) => new LTimeLiteralValue(((LTimeLiteralValue)args[0]).Value.CheckedNeg(), result);
		private static ILiteralValue ModLTIME(IType result, ILiteralValue[] args) => new LTimeLiteralValue(((LTimeLiteralValue)args[0]).Value.CheckedMod(((LTimeLiteralValue)args[1]).Value), result);

		private static ILiteralValue AddTIME(IType result, ILiteralValue[] args) => new TimeLiteralValue(((TimeLiteralValue)args[0]).Value.CheckedAdd(((TimeLiteralValue)args[1]).Value), result);
		private static ILiteralValue SubTIME(IType result, ILiteralValue[] args) => new TimeLiteralValue(((TimeLiteralValue)args[0]).Value.CheckedSub(((TimeLiteralValue)args[1]).Value), result);
		private static ILiteralValue NegTIME(IType result, ILiteralValue[] args) => new TimeLiteralValue(((TimeLiteralValue)args[0]).Value.CheckedNeg(), result);
		private static ILiteralValue ModTIME(IType result, ILiteralValue[] args) => new TimeLiteralValue(((TimeLiteralValue)args[0]).Value.CheckedMod(((TimeLiteralValue)args[1]).Value), result);

		private static ILiteralValue ArithmeticCast_FromInt(IAnyIntLiteralValue intLiteralValue, IType targetType, SystemScope systemScope)
		{
			var resultValue = systemScope.TryCreateLiteralFromIntValue(intLiteralValue.Value, targetType);
			if (resultValue == null)
				throw new OverflowException();
			else
				return resultValue;
		}
		private static ILiteralValue Real_To_LReal(IType result, ILiteralValue[] args)
			=> new LRealLiteralValue(((RealLiteralValue)args[0]).Value, result);

		public bool TryGetConstantEvaluator(FunctionVariableSymbol functionSymbol, [NotNullWhen(true)] out Func<IType, ILiteralValue[], ILiteralValue>? result)
			=> Table.TryGetValue(functionSymbol, out result) && result != null;
		public SymbolSet<FunctionVariableSymbol> AllFunctions { get; }
		public OperatorFunction? TryGetOperatorFunction((string Name, bool IsGenericReturn) op, BuiltInType type)
		{
			var opName = OperatorName(op.Name, type);
			if (AllFunctions.TryGetValue(opName, out var func))
				return new(func, op.IsGenericReturn);
			else
				return default;
		}
		public OperatorFunction? TryGetBinaryOperatorFunction(IBinaryOperatorToken token, BuiltInType type)
		{
			var op = token.Accept(BinaryOperatorMap.Instance);
			return TryGetOperatorFunction(op, type);
		}
		public OperatorFunction? TryGetUnaryOperatorFunction(IUnaryOperatorToken token, BuiltInType type)
		{
			var op = token.Accept(UnaryOperatorMap.Instance);
			return TryGetOperatorFunction(op, type);
		}
		public string GetBinaryOperatorFunctionName(IBinaryOperatorToken token)
			=> token.Accept(BinaryOperatorMap.Instance).Name;
		public string GetUnaryOperatorFunctionName(IUnaryOperatorToken token)
			=> token.Accept(UnaryOperatorMap.Instance).Name;

		public FunctionVariableSymbol? TryGetCastFunction(BuiltInType from, BuiltInType to)
		{
			var op = CastFunctionName(from, to);
			return AllFunctions.TryGetValue(op);
		}

		private sealed class BinaryOperatorMap : IBinaryOperatorToken.IVisitor<(string Name, bool IsGenericReturn)>
		{
			public static readonly BinaryOperatorMap Instance = new();

			public (string, bool) Visit(EqualToken equalToken) => ("EQUAL", false);
			public (string, bool) Visit(LessEqualToken lessEqualToken) => ("LESS_EQUAL", false);
			public (string, bool) Visit(LessToken lessToken) => ("LESS", false);
			public (string, bool) Visit(GreaterToken greaterToken) => ("GREATER", false);
			public (string, bool) Visit(GreaterEqualToken greaterEqualToken) => ("GREATER_EQUAL", false);
			public (string, bool) Visit(UnEqualToken unEqualToken) => ("NOT_EQUAL", false);

			public (string, bool) Visit(PlusToken plusToken) => ("ADD", true);
			public (string, bool) Visit(MinusToken minusToken) => ("SUB", true);
			public (string, bool) Visit(StarToken starToken) => ("MUL", true);
			public (string, bool) Visit(SlashToken slashToken) => ("DIV", true);
			public (string, bool) Visit(ModToken modToken) => ("MOD", true);
			public (string, bool) Visit(PowerToken powerToken) => ("POW", true);

			public (string, bool) Visit(AndToken andToken) => ("AND", true);
			public (string, bool) Visit(XorToken xorToken) => ("XOR", true);
			public (string, bool) Visit(OrToken orToken) => ("OR", true);
		}

		private sealed class UnaryOperatorMap : IUnaryOperatorToken.IVisitor<(string Name, bool IsGenericReturn)>
		{
			public static readonly UnaryOperatorMap Instance = new();

			public (string Name, bool IsGenericReturn) Visit(MinusToken minusToken) => ("NEG", true);
			public (string Name, bool IsGenericReturn) Visit(NotToken notToken) => ("NOT", true);
		}
	}

	public readonly struct OperatorFunction
	{
		public readonly FunctionVariableSymbol Symbol;
		/// Is the operator "generic", i.e. Is the return type equal to the argument types.
		/// This is only relevant for alias types.
		public readonly bool IsGenericReturn;

		public OperatorFunction(FunctionVariableSymbol symbol, bool isGenericReturn)
		{
			Symbol = symbol;
			IsGenericReturn = isGenericReturn;
		}
	}
}
