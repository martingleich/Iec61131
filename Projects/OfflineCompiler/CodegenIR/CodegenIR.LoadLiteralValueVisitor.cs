using System;
using Compiler;
using IR = Runtime.IR;

namespace OfflineCompiler
{

	public sealed partial class CodegenIR
	{
		private sealed class LoadLiteralValueVisitor : ILiteralValue.IVisitor<IR.LiteralExpression>
		{
			public static readonly LoadLiteralValueVisitor Instance = new();
			public IR.LiteralExpression Visit(TimeLiteralValue timeLiteralValue) => IR.LiteralExpression.Signed32(timeLiteralValue.Value.Milliseconds);
			public IR.LiteralExpression Visit(LTimeLiteralValue lTimeLiteralValue) => IR.LiteralExpression.Signed64(lTimeLiteralValue.Value.Nanoseconds);
			public IR.LiteralExpression Visit(NullPointerLiteralValue nullPointerLiteralValue) => IR.LiteralExpression.NullPointer;
			public IR.LiteralExpression Visit(LRealLiteralValue lRealLiteralValue) => IR.LiteralExpression.Float64(lRealLiteralValue.Value);
			public IR.LiteralExpression Visit(RealLiteralValue realLiteralValue) => IR.LiteralExpression.Float32(realLiteralValue.Value);
			public IR.LiteralExpression Visit(EnumLiteralValue enumLiteralValue) => enumLiteralValue.InnerValue.Accept(this);
			public IR.LiteralExpression Visit(BooleanLiteralValue booleanLiteralValue) => IR.LiteralExpression.Bool(booleanLiteralValue.Value);
			public IR.LiteralExpression Visit(LIntLiteralValue lIntLiteralValue) => IR.LiteralExpression.Signed64(lIntLiteralValue.Value);
			public IR.LiteralExpression Visit(ULIntLiteralValue uLIntLiteralValue) => IR.LiteralExpression.Bits64(uLIntLiteralValue.Value);
			public IR.LiteralExpression Visit(DIntLiteralValue dIntLiteralValue) => IR.LiteralExpression.Signed32(dIntLiteralValue.Value);
			public IR.LiteralExpression Visit(UDIntLiteralValue uDIntLiteralValue) => IR.LiteralExpression.Bits32(uDIntLiteralValue.Value);
			public IR.LiteralExpression Visit(IntLiteralValue intLiteralValue) => IR.LiteralExpression.Signed16(intLiteralValue.Value);
			public IR.LiteralExpression Visit(UIntLiteralValue uIntLiteralValue) => IR.LiteralExpression.Bits16(uIntLiteralValue.Value);
			public IR.LiteralExpression Visit(USIntLiteralValue uSIntLiteralValue) => IR.LiteralExpression.Bits8(uSIntLiteralValue.Value);
			public IR.LiteralExpression Visit(SIntLiteralValue sIntLiteralValue) => IR.LiteralExpression.Signed8(sIntLiteralValue.Value);

			public IR.LiteralExpression Visit(UnknownLiteralValue unknownLiteralValue) => throw new InvalidOperationException();
		}
	}
}
