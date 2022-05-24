using System;
using Compiler;
using IRExpr = Runtime.IR.Expressions;

namespace Compiler.CodegenIR
{

	public sealed partial class CodegenIR
	{
		public sealed class LoadLiteralValueVisitor : ILiteralValue.IVisitor<IRExpr.LiteralExpression>
		{
			public static readonly LoadLiteralValueVisitor Instance = new();
			public IRExpr.LiteralExpression Visit(TimeLiteralValue timeLiteralValue) => IRExpr.LiteralExpression.Signed32(timeLiteralValue.Value.Milliseconds);
			public IRExpr.LiteralExpression Visit(LTimeLiteralValue lTimeLiteralValue) => IRExpr.LiteralExpression.Signed64(lTimeLiteralValue.Value.Nanoseconds);
			public IRExpr.LiteralExpression Visit(NullPointerLiteralValue nullPointerLiteralValue) => IRExpr.LiteralExpression.NullPointer;
			public IRExpr.LiteralExpression Visit(LRealLiteralValue lRealLiteralValue) => IRExpr.LiteralExpression.Float64(lRealLiteralValue.Value);
			public IRExpr.LiteralExpression Visit(RealLiteralValue realLiteralValue) => IRExpr.LiteralExpression.Float32(realLiteralValue.Value);
			public IRExpr.LiteralExpression Visit(EnumLiteralValue enumLiteralValue) => enumLiteralValue.InnerValue.Accept(this);
			public IRExpr.LiteralExpression Visit(BooleanLiteralValue booleanLiteralValue) => IRExpr.LiteralExpression.Bool(booleanLiteralValue.Value);
			public IRExpr.LiteralExpression Visit(LIntLiteralValue lIntLiteralValue) => IRExpr.LiteralExpression.Signed64(lIntLiteralValue.Value);
			public IRExpr.LiteralExpression Visit(ULIntLiteralValue uLIntLiteralValue) => IRExpr.LiteralExpression.Bits64(uLIntLiteralValue.Value);
			public IRExpr.LiteralExpression Visit(DIntLiteralValue dIntLiteralValue) => IRExpr.LiteralExpression.Signed32(dIntLiteralValue.Value);
			public IRExpr.LiteralExpression Visit(UDIntLiteralValue uDIntLiteralValue) => IRExpr.LiteralExpression.Bits32(uDIntLiteralValue.Value);
			public IRExpr.LiteralExpression Visit(IntLiteralValue intLiteralValue) => IRExpr.LiteralExpression.Signed16(intLiteralValue.Value);
			public IRExpr.LiteralExpression Visit(UIntLiteralValue uIntLiteralValue) => IRExpr.LiteralExpression.Bits16(uIntLiteralValue.Value);
			public IRExpr.LiteralExpression Visit(USIntLiteralValue uSIntLiteralValue) => IRExpr.LiteralExpression.Bits8(uSIntLiteralValue.Value);
			public IRExpr.LiteralExpression Visit(SIntLiteralValue sIntLiteralValue) => IRExpr.LiteralExpression.Signed8(sIntLiteralValue.Value);

			public IRExpr.LiteralExpression Visit(UnknownLiteralValue unknownLiteralValue) => throw new InvalidOperationException();
		}
	}
}
