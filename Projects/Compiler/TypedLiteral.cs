namespace Compiler
{
	public readonly struct TypedLiteral
	{
		public readonly IBuiltInTypeToken Type;
		public readonly ILiteralToken LiteralToken;

		public TypedLiteral(IBuiltInTypeToken type, ILiteralToken literalToken)
		{
			Type = type;
			LiteralToken = literalToken;
		}

		public override string? ToString() => $"{Type}#{LiteralToken}";
	}
}
