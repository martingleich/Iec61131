namespace Compiler.Types
{
	public sealed class NullType : IType
	{
		public static readonly NullType Instance = new ();
		public LayoutInfo LayoutInfo => LayoutInfo.Zero;
		public string Code => ImplicitName.NullType.Original;
		public T Accept<T, TContext>(IType.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);

		public override string ToString() => Code;
	}
}