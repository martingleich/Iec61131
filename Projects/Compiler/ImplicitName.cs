namespace Compiler
{
	public static class ImplicitName
	{
		public static readonly CaseInsensitiveString NullType = "__Null_Type".ToCaseInsensitive();
		public static readonly CaseInsensitiveString ErrorFunction = "__Error_Function".ToCaseInsensitive();
		public static CaseInsensitiveString ErrorParam(int id) => $"__Error_Param_{id}".ToCaseInsensitive();
		public static CaseInsensitiveString ErrorBinaryOperator(string leftType, string rightType, string op)
			=> $"__Error_{op}_{leftType}_{rightType}".ToCaseInsensitive();
		public static CaseInsensitiveString ErrorUnaryOperator(string valueType, string op)
			=> $"__Error_{op}_{valueType}".ToCaseInsensitive();
	}
}
