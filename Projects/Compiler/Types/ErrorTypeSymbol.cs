namespace Compiler.Types
{
	public sealed class ErrorTypeSymbol : ITypeSymbol
	{
		public ErrorTypeSymbol(SourceSpan declaringSpan, CaseInsensitiveString name)
		{
			UniqueId = new UniqueSymbolId(CaseInsensitiveString.Empty, Name);
			DeclaringSpan = declaringSpan;
		}

		public LayoutInfo LayoutInfo => new(0, 1);
		public CaseInsensitiveString Name => UniqueId.Name;
		public string Code => Name.Original;
		public SourceSpan DeclaringSpan { get; }
		public UniqueSymbolId UniqueId {get;}

		public T Accept<T, TContext>(IType.IVisitor<T, TContext> visitor, TContext context) => visitor.VisitError(context);

		public override string ToString() => Code;
	}
}