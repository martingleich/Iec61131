using Compiler.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Compiler
{
	public sealed class BuiltInFunctionTable
	{
		private readonly ImmutableDictionary<FunctionSymbol, Func<IType, ILiteralValue[], ILiteralValue>?> Table;

		private static FunctionSymbol BinaryOperator(string baseName, BuiltInType type)
			=> BinaryOperator(baseName, type, type);
		private static FunctionSymbol BinaryOperator(string baseName, BuiltInType type, BuiltInType returnType)
		{
			var name = (baseName + "_" + type.Name).ToCaseInsensitive();
			return new(isProgram: false, name, default, OrderedSymbolSet.ToOrderedSymbolSet<ParameterSymbol>(
				new(ParameterKind.Input, default, "LEFT".ToCaseInsensitive(), type),
				new(ParameterKind.Input, default, "RIGHT".ToCaseInsensitive(), type),
				new(ParameterKind.Output, default, name, returnType)));
		}
		private static FunctionSymbol UnaryOperator(string baseName, BuiltInType type)
		{
			var name = (baseName + "_" + type.Name).ToCaseInsensitive();
			return new(isProgram: false, name, default, OrderedSymbolSet.ToOrderedSymbolSet<ParameterSymbol>(
				new(ParameterKind.Input, default, "VALUE".ToCaseInsensitive(), type),
				new(ParameterKind.Output, default, name, type)));
		}

		public BuiltInFunctionTable(SystemScope systemScope)
		{
			var builder = ImmutableDictionary.CreateBuilder<FunctionSymbol, Func<IType, ILiteralValue[], ILiteralValue>?>(SymbolByNameComparer<FunctionSymbol>.Instance);

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
			AddComparisons(builder, systemScope.Real, systemScope.Bool);
			AddComparisons(builder, systemScope.LReal, systemScope.Bool);

			AddEquality(builder, systemScope.Bool, systemScope.Bool, (x, y) => ((BooleanLiteralValue)x).Value == ((BooleanLiteralValue)y).Value);
			builder.Add(BinaryOperator("AND", systemScope.Bool), AndBool);
			builder.Add(BinaryOperator("OR", systemScope.Bool), OrBool);
			builder.Add(BinaryOperator("XOR", systemScope.Bool), XorBool);
			builder.Add(UnaryOperator("NOT", systemScope.Bool), NotBool);

			Table = builder.ToImmutable();
			AllFunctions = Table.Keys.ToSymbolSet();
		}

		private static void AddComparisons(
			ImmutableDictionary<FunctionSymbol, Func<IType, ILiteralValue[], ILiteralValue>?>.Builder builder,
			BuiltInType type,
			BuiltInType boolean,
			Func<ILiteralValue, ILiteralValue, bool> lessEqual)
		{
			ILiteralValue Equal(IType result, ILiteralValue[] args) => new BooleanLiteralValue(lessEqual(args[0], args[1]) && lessEqual(args[1], args[0]), result);
			ILiteralValue NotEqual(IType result, ILiteralValue[] args) => new BooleanLiteralValue(!lessEqual(args[0], args[1]) || !lessEqual(args[1], args[0]), result);
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
			ImmutableDictionary<FunctionSymbol, Func<IType, ILiteralValue[], ILiteralValue>?>.Builder builder,
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
			ImmutableDictionary<FunctionSymbol, Func<IType, ILiteralValue[], ILiteralValue>?>.Builder builder,
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

		public bool TryGetConstantEvaluator(FunctionSymbol functionSymbol, [NotNullWhen(true)] out Func<IType, ILiteralValue[], ILiteralValue>? result)
			=> Table.TryGetValue(functionSymbol, out result) && result != null;
		public SymbolSet<FunctionSymbol> AllFunctions { get; }
		private OperatorFunction? TryGetOperatorFunction((string Name, bool IsGenericReturn) op, BuiltInType type)
		{
			if (AllFunctions.TryGetValue($"{op.Name}_{type.Name}".ToCaseInsensitive()) is FunctionSymbol func)
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
		public readonly FunctionSymbol Symbol;
		/// Is the operator "generic", i.e. Is the return type equal to the argument types.
		/// This is only relevant for alias types.
		public readonly bool IsGenericReturn;

		public OperatorFunction(FunctionSymbol symbol, bool isGenericReturn)
		{
			Symbol = symbol;
			IsGenericReturn = isGenericReturn;
		}
	}
}
