namespace Compiler.Types
{
	public sealed class FunctionTypeSymbol : ICallableTypeSymbol
	{
		public readonly bool IsError;
		public CaseInsensitiveString Name { get; }
		public SourcePosition DeclaringPosition { get; }
		public OrderedSymbolSet<ParameterVariableSymbol> Parameters { get; }
		public LayoutInfo LayoutInfo => new (0, 1);
		public string Code => Name.Original;

		public FunctionTypeSymbol(CaseInsensitiveString name, SourcePosition declaringPosition, OrderedSymbolSet<ParameterVariableSymbol> parameters) :
			this(false, name, declaringPosition, parameters)
		{
		}
		private FunctionTypeSymbol(bool isError, CaseInsensitiveString name, SourcePosition declaringPosition, OrderedSymbolSet<ParameterVariableSymbol> parameters)
		{
			IsError = isError;
			Name = name;
			DeclaringPosition = declaringPosition;
			Parameters = parameters;
		}

		public override string ToString() => $"{Name}";

		public static FunctionTypeSymbol CreateError(SourcePosition sourcePosition)
			=> CreateError(sourcePosition, ImplicitName.ErrorFunction, ITypeSymbol.CreateErrorForFunc(sourcePosition, ImplicitName.ErrorFunction));
		public static FunctionTypeSymbol CreateError(SourcePosition sourcePosition, CaseInsensitiveString name)
			=> CreateError(sourcePosition, name, ITypeSymbol.CreateErrorForFunc(sourcePosition, name));
		public static FunctionTypeSymbol CreateError(SourcePosition sourcePosition, IType returnType)
			=> CreateError(sourcePosition, ImplicitName.ErrorFunction, returnType);
		public static FunctionTypeSymbol CreateError(SourcePosition sourcePosition, CaseInsensitiveString name, IType returnType)
			=> new(true, name, sourcePosition, OrderedSymbolSet.ToOrderedSymbolSet(
				new ParameterVariableSymbol(ParameterKind.Output, sourcePosition, name, returnType)));

		public T Accept<T, TContext>(IType.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
	}
}