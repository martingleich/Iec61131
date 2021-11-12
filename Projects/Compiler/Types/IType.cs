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
			T Visit(StructuredTypeSymbol structuredTypeSymbol, TContext context);
			T Visit(BuiltInType builtInTypeSymbol, TContext context);
			T Visit(PointerType pointerTypeSymbol, TContext context);
			T Visit(StringType stringTypeSymbol, TContext context);
			T Visit(ArrayType arrayTypeSymbol, TContext context);
			T Visit(EnumTypeSymbol enumTypeSymbol, TContext context);
			T Visit(AliasTypeSymbol aliasTypeSymbol, TContext context);
			T Visit(NullType nullType, TContext context);
			T Visit(FunctionBlockSymbol functionBlockSymbol, TContext context);
		}
	}
}