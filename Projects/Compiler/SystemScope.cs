using System;
using System.Collections.Immutable;
using System.Linq;
using Compiler.Types;

namespace Compiler
{
	public sealed class SystemScope
	{
		private static FunctionSymbol BinaryOperator(string name, IType leftType, IType rightType, IType returnType)
			=> new(name.ToCaseInsensitive(), default, OrderedSymbolSet.ToOrderedSymbolSet<ParameterSymbol>(
				new(ParameterKind.Input, default, "LEFT_VALUE".ToCaseInsensitive(), leftType),
				new(ParameterKind.Input, default, "RIGHT_VALUE".ToCaseInsensitive(), rightType),
				new(ParameterKind.Output, default, name.ToCaseInsensitive(), returnType)));

		public readonly SymbolSet<FunctionSymbol> AllBuiltInFunctions;

		public readonly ImmutableArray<BuiltInType> AllBuiltInTypes;
		public readonly ImmutableArray<BuiltInType> ArithmeticTypes;
		public readonly BuiltInType Char = new(1, 1, "Char");
		public readonly BuiltInType LReal = new(8, 8, "LReal", BuiltInType.Flag.Arithmetic);
		public readonly BuiltInType Real = new(4, 4, "Real", BuiltInType.Flag.Arithmetic);
		public readonly BuiltInType LInt = new(8, 8, "LInt", BuiltInType.Flag.Arithmetic);
		public readonly BuiltInType DInt = new(4, 4, "DInt", BuiltInType.Flag.Arithmetic);
		public readonly BuiltInType Int = new(2, 2, "Int", BuiltInType.Flag.Arithmetic);
		public readonly BuiltInType SInt = new(2, 2, "SInt", BuiltInType.Flag.Arithmetic);
		public readonly BuiltInType ULInt = new(8, 8, "ULInt", BuiltInType.Flag.Arithmetic | BuiltInType.Flag.Unsigned);
		public readonly BuiltInType UDInt = new(4, 4, "UDInt", BuiltInType.Flag.Arithmetic | BuiltInType.Flag.Unsigned);
		public readonly BuiltInType UInt = new(2, 2, "UInt", BuiltInType.Flag.Arithmetic | BuiltInType.Flag.Unsigned);
		public readonly BuiltInType USInt = new(1, 1, "USInt", BuiltInType.Flag.Arithmetic | BuiltInType.Flag.Unsigned);
		public readonly BuiltInType LWord = new(8, 8, "LWord");
		public readonly BuiltInType DWord = new(4, 4, "DWord");
		public readonly BuiltInType Word = new(2, 2, "Word");
		public readonly BuiltInType Byte = new(1, 1, "Byte");
		public readonly BuiltInType Bool = new(1, 1, "Bool");
		public readonly BuiltInType LTime = new(8, 8, "LTime");
		public readonly BuiltInType Time = new(4, 4, "Time");
		public readonly BuiltInType LDT = new(8, 8, "LDT");
		public readonly BuiltInType DT = new(4, 4, "DT");
		public readonly BuiltInType LDate = new(8, 8, "LDate");
		public readonly BuiltInType Date = new(4, 4, "Date");
		public readonly BuiltInType LTOD = new(8, 8, "LTOD");
		public readonly BuiltInType TOD = new(4, 4, "TOD");

		private readonly TypeMapper BuiltInTypeMapper;

		public SystemScope()
		{
			AllBuiltInTypes = ImmutableArray.Create(Char, LReal, Real, LInt, DInt, Int, SInt, ULInt, UDInt, UInt, USInt, LWord, DWord, Word, Byte, Bool, LTime, Time, LDT, DT, LDate, Date, LTOD, TOD);
			ArithmeticTypes = AllBuiltInTypes.Where(t => t.IsArithmetic).ToImmutableArray();
			BuiltInTypeMapper = new TypeMapper(this);

			var numericOperators = new[] { "ADD", "SUB", "MUL", "DIV" };
			AllBuiltInFunctions = (from type in ArithmeticTypes
						 from op in numericOperators
						 select BinaryOperator($"{op}_{type.Name}", type, type, type)).ToSymbolSet();
		}

		public FunctionSymbol GetOperatorFunction(string op, BuiltInType type)
			=> AllBuiltInFunctions[$"{op}_{type.Name}"];
		public BuiltInType MapTokenToType(IBuiltInTypeToken token) => token.Accept(BuiltInTypeMapper);

		private sealed class TypeMapper : IBuiltInTypeToken.IVisitor<BuiltInType>
		{
			private readonly SystemScope SystemScope;

			public TypeMapper(SystemScope systemScope)
			{
				SystemScope = systemScope ?? throw new ArgumentNullException(nameof(systemScope));
			}

			public BuiltInType Visit(CharToken charToken) => SystemScope.Char;
			public BuiltInType Visit(LRealToken lRealToken) => SystemScope.LReal;
			public BuiltInType Visit(RealToken realToken) => SystemScope.Real;
			public BuiltInType Visit(LIntToken lIntToken) => SystemScope.LInt;
			public BuiltInType Visit(DIntToken dIntToken) => SystemScope.DInt;
			public BuiltInType Visit(IntToken intToken) => SystemScope.Int;
			public BuiltInType Visit(SIntToken sIntToken) => SystemScope.SInt;
			public BuiltInType Visit(ULIntToken uLIntToken) => SystemScope.ULInt;
			public BuiltInType Visit(UDIntToken uDIntToken) => SystemScope.UDInt;
			public BuiltInType Visit(UIntToken uIntToken) => SystemScope.UInt;
			public BuiltInType Visit(USIntToken uSIntToken) => SystemScope.USInt;
			public BuiltInType Visit(LWordToken lWordToken) => SystemScope.LWord;
			public BuiltInType Visit(DWordToken dWordToken) => SystemScope.DWord;
			public BuiltInType Visit(WordToken wordToken) => SystemScope.Word;
			public BuiltInType Visit(ByteToken byteToken) => SystemScope.Byte;
			public BuiltInType Visit(BoolToken boolToken) => SystemScope.Bool;
			public BuiltInType Visit(LTimeToken lTimeToken) => SystemScope.LTime;
			public BuiltInType Visit(TimeToken timeToken) => SystemScope.Time;
			public BuiltInType Visit(LDTToken lDTToken) => SystemScope.LDT;
			public BuiltInType Visit(DTToken dTToken) => SystemScope.DT;
			public BuiltInType Visit(LDateToken lDateToken) => SystemScope.LDate;
			public BuiltInType Visit(DateToken dateToken) => SystemScope.Date;
			public BuiltInType Visit(LTODToken lTODToken) => SystemScope.LTOD;
			public BuiltInType Visit(TODToken tODToken) => SystemScope.TOD;
		}
	}
}
