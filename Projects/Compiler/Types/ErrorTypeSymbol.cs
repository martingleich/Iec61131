namespace Compiler.Types
{
	public sealed class ErrorTypeSymbol : ITypeSymbol
	{
		public ErrorTypeSymbol(SourceSpan declaringSpan, CaseInsensitiveString name)
		{
			UniqueName = new UniqueSymbolId(CaseInsensitiveString.Empty, Name);
			DeclaringSpan = declaringSpan;
		}

		public LayoutInfo LayoutInfo => new(0, 1);
		public CaseInsensitiveString Name => UniqueName.Name;
		public string Code => Name.Original;
		public SourceSpan DeclaringSpan { get; }
		public UniqueSymbolId UniqueName {get;}

		public T Accept<T, TContext>(IType.IVisitor<T, TContext> visitor, TContext context) => visitor.VisitError(context);

		public override string ToString() => Code;
	}
}