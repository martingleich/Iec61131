namespace Compiler.Types
{
	public static class TypeExtensions
	{
		public static bool IsError(this IType self) => self is ErrorTypeSymbol || self is FunctionTypeSymbol fun && fun.IsError;
	}

}