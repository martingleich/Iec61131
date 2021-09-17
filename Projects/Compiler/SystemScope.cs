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

		private readonly SymbolSet<FunctionSymbol> Functions;
		public SystemScope()
		{
			var numericTypes = new[]{
				BuiltInType.SInt, BuiltInType.USInt,
				BuiltInType.Int, BuiltInType.UInt,
				BuiltInType.DInt, BuiltInType.UDInt,
				BuiltInType.LInt, BuiltInType.ULInt,
				BuiltInType.Byte, BuiltInType.Word, BuiltInType.DWord, BuiltInType.LWord,
				BuiltInType.Real, BuiltInType.LReal };
			var numericOperators = new[] { "ADD", "SUB", "MUL", "DIV" };
			Functions = (from type in numericTypes
							 from op in numericOperators
							 select BinaryOperator($"{op}_{type.Name}", type, type, type)).ToSymbolSet();
		}

		public FunctionSymbol GetOperatorFunction(string op, BuiltInType type)
			=> Functions[$"{op}_{type.Name}"];
	}
}
