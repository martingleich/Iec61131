using Compiler.Types;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Compiler
{
	public sealed class BuiltInFunctionTable
	{
		private readonly ImmutableDictionary<FunctionSymbol, Func<IType, ILiteralValue[], ILiteralValue>?> Table;

		private static FunctionSymbol BinaryOperator(string name, BuiltInType type)
			=> new(isProgram: false, (name + "_" + type.Name).ToCaseInsensitive(), default, OrderedSymbolSet.ToOrderedSymbolSet<ParameterSymbol>(
				new(ParameterKind.Input, default, "LEFT_VALUE".ToCaseInsensitive(), type),
				new(ParameterKind.Input, default, "RIGHT_VALUE".ToCaseInsensitive(), type),
				new(ParameterKind.Output, default, name.ToCaseInsensitive(), type)));

		public struct BuiltInId
		{
			public readonly string Id;

			public BuiltInId(string id)
			{
				Id = id ?? throw new ArgumentNullException(nameof(id));
			}

			public override string ToString() => Id;
		}

		public BuiltInFunctionTable(SystemScope systemScope)
		{
			var builder = ImmutableDictionary.CreateBuilder<FunctionSymbol, Func<IType, ILiteralValue[], ILiteralValue>?>(SymbolByNameComparer<FunctionSymbol>.Instance);

			builder.Add(BinaryOperator("ADD", systemScope.SInt), AddSINT);
			builder.Add(BinaryOperator("SUB", systemScope.SInt), SubSINT);
			builder.Add(BinaryOperator("MUL", systemScope.SInt), MulSINT);
			builder.Add(BinaryOperator("DIV", systemScope.SInt), DivSINT);
			builder.Add(BinaryOperator("MOD", systemScope.SInt), ModSINT);

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

			builder.Add(BinaryOperator("ADD", systemScope.LReal), null);
			builder.Add(BinaryOperator("SUB", systemScope.LReal), null);
			builder.Add(BinaryOperator("MUL", systemScope.LReal), null);
			builder.Add(BinaryOperator("DIV", systemScope.LReal), null);
			builder.Add(BinaryOperator("MOD", systemScope.LReal), null);

			Table = builder.ToImmutable();
			AllFunctions = Table.Keys.ToSymbolSet();
		}
		private static ILiteralValue AddSINT(IType result, ILiteralValue[] args) => new SIntLiteralValue(checked((sbyte)(((SIntLiteralValue)args[0]).Value + ((SIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue SubSINT(IType result, ILiteralValue[] args) => new SIntLiteralValue(checked((sbyte)(((SIntLiteralValue)args[0]).Value - ((SIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue MulSINT(IType result, ILiteralValue[] args) => new SIntLiteralValue(checked((sbyte)(((SIntLiteralValue)args[0]).Value * ((SIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue DivSINT(IType result, ILiteralValue[] args) => new SIntLiteralValue(checked((sbyte)(((SIntLiteralValue)args[0]).Value / ((SIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue ModSINT(IType result, ILiteralValue[] args) => new SIntLiteralValue(checked((sbyte)(((SIntLiteralValue)args[0]).Value % ((SIntLiteralValue)args[1]).Value)), result);

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

		private static ILiteralValue AddUINT(IType result, ILiteralValue[] args) => new UIntLiteralValue(checked((ushort)(((UIntLiteralValue)args[0]).Value + ((UIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue SubUINT(IType result, ILiteralValue[] args) => new UIntLiteralValue(checked((ushort)(((UIntLiteralValue)args[0]).Value - ((UIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue MulUINT(IType result, ILiteralValue[] args) => new UIntLiteralValue(checked((ushort)(((UIntLiteralValue)args[0]).Value * ((UIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue DivUINT(IType result, ILiteralValue[] args) => new UIntLiteralValue(checked((ushort)(((UIntLiteralValue)args[0]).Value / ((UIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue ModUINT(IType result, ILiteralValue[] args) => new UIntLiteralValue(checked((ushort)(((UIntLiteralValue)args[0]).Value % ((UIntLiteralValue)args[1]).Value)), result);

		private static ILiteralValue AddDINT(IType result, ILiteralValue[] args) => new DIntLiteralValue(checked((int)(((IntLiteralValue)args[0]).Value + ((IntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue SubDINT(IType result, ILiteralValue[] args) => new DIntLiteralValue(checked((int)(((IntLiteralValue)args[0]).Value - ((IntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue MulDINT(IType result, ILiteralValue[] args) => new DIntLiteralValue(checked((int)(((IntLiteralValue)args[0]).Value * ((IntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue DivDINT(IType result, ILiteralValue[] args) => new DIntLiteralValue(checked((int)(((IntLiteralValue)args[0]).Value / ((IntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue ModDINT(IType result, ILiteralValue[] args) => new DIntLiteralValue(checked((int)(((IntLiteralValue)args[0]).Value % ((IntLiteralValue)args[1]).Value)), result);

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

		private static ILiteralValue AddULINT(IType result, ILiteralValue[] args) => new ULIntLiteralValue(checked((ulong)(((ULIntLiteralValue)args[0]).Value + ((ULIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue SubULINT(IType result, ILiteralValue[] args) => new ULIntLiteralValue(checked((ulong)(((ULIntLiteralValue)args[0]).Value - ((ULIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue MulULINT(IType result, ILiteralValue[] args) => new ULIntLiteralValue(checked((ulong)(((ULIntLiteralValue)args[0]).Value * ((ULIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue DivULINT(IType result, ILiteralValue[] args) => new ULIntLiteralValue(checked((ulong)(((ULIntLiteralValue)args[0]).Value / ((ULIntLiteralValue)args[1]).Value)), result);
		private static ILiteralValue ModULINT(IType result, ILiteralValue[] args) => new ULIntLiteralValue(checked((ulong)(((ULIntLiteralValue)args[0]).Value % ((ULIntLiteralValue)args[1]).Value)), result);

		public bool TryGetConstantEvaluator(FunctionSymbol functionSymbol, [NotNullWhen(true)] out Func<IType, ILiteralValue[], ILiteralValue>? result)
			=> Table.TryGetValue(functionSymbol, out result);
		public SymbolSet<FunctionSymbol> AllFunctions { get; }
		public FunctionSymbol GetOperatorFunction(BuiltInId op, BuiltInType type)
			=> AllFunctions[$"{op.Id}_{type.Name}"];
		public BuiltInId? MapBinaryOperatorToOpId(IBinaryOperatorToken token) => token.Accept(BinaryOperatorMap.Instance);

		private sealed class BinaryOperatorMap : IBinaryOperatorToken.IVisitor<BuiltInId?>
		{
			public static readonly BinaryOperatorMap Instance = new();

			public BuiltInId? Visit(EqualToken equalToken) => null;
			public BuiltInId? Visit(LessEqualToken lessEqualToken) => null;
			public BuiltInId? Visit(LessToken lessToken) => null;
			public BuiltInId? Visit(GreaterToken greaterToken) => null;
			public BuiltInId? Visit(GreaterEqualToken greaterEqualToken) => null;
			public BuiltInId? Visit(UnEqualToken unEqualToken) => null;

			public BuiltInId? Visit(PlusToken plusToken) => new BuiltInId("ADD");
			public BuiltInId? Visit(MinusToken minusToken) => new BuiltInId("SUB");
			public BuiltInId? Visit(StarToken starToken) => new BuiltInId("MUL");
			public BuiltInId? Visit(SlashToken slashToken) => new BuiltInId("DIV");
			public BuiltInId? Visit(ModToken modToken) => new BuiltInId("MOD");
			public BuiltInId? Visit(PowerToken powerToken) => null;

			public BuiltInId? Visit(AndToken andToken) => null;
			public BuiltInId? Visit(XorToken xorToken) => null;
			public BuiltInId? Visit(OrToken orToken) => null;
		}
	}
}
