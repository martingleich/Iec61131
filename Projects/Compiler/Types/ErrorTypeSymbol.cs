namespace Compiler.Types
{
	public sealed class ErrorTypeSymbol : ITypeSymbol
	{
		public ErrorTypeSymbol(SourcePosition declaringPosition, CaseInsensitiveString name)
		{
			Name = name;
			DeclaringPosition = declaringPosition;
		}

		public LayoutInfo LayoutInfo => new(0, 1);
		public CaseInsensitiveString Name { get; }
		public string Code => Name.Original;
		public SourcePosition DeclaringPosition { get; }

		public T Accept<T, TContext>(IType.IVisitor<T, TContext> visitor, TContext context) => visitor.VisitError(context);

		public override string ToString() => Code;
	}
}