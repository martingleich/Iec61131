using Runtime.IR.RuntimeTypes;

namespace Compiler.Types
{
	public interface IType
	{
		LayoutInfo LayoutInfo { get; }
		string Code { get; }
		T Accept<T, TContext>(IVisitor<T, TContext> visitor, TContext context);

		interface IVisitor<T, TContext>
		{
			T VisitError(TContext context);
			T Visit(ITypeSymbol typeSymbol, TContext context);
			T Visit(BuiltInType builtInType, TContext context);
			T Visit(PointerType pointerType, TContext context);
			T Visit(StringType stringType, TContext context);
			T Visit(ArrayType arrayType, TContext context);
			T Visit(NullType nullType, TContext context);
		}
	}
}