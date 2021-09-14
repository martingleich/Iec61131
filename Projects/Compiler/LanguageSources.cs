namespace Compiler
{
	public sealed class TopLevelInterfaceAndBodyPouLanguageSource : ILanguageSource
	{
		public readonly PouInterfaceSyntax Interface;
		public readonly StatementListSyntax Body;

		public TopLevelInterfaceAndBodyPouLanguageSource(PouInterfaceSyntax @interface, StatementListSyntax body)
		{
			Interface = @interface;
			Body = body;
		}

		void ILanguageSource.Accept(ILanguageSource.IVisitor visitor) => visitor.Visit(this);
	}

	public sealed class GlobalVariableLanguageSource : ILanguageSource
	{
		public readonly string Name;
		public readonly GlobalVarListSyntax Syntax;

		public GlobalVariableLanguageSource(string name, GlobalVarListSyntax syntax)
		{
			Name = name;
			Syntax = syntax;
		}

		void ILanguageSource.Accept(ILanguageSource.IVisitor visitor) => visitor.Visit(this);
	}

	public sealed class DutLanguageSource : ILanguageSource
	{
		public readonly TypeDeclarationSyntax Syntax;

		public DutLanguageSource(TypeDeclarationSyntax syntax)
		{
			Syntax = syntax;
		}

		void ILanguageSource.Accept(ILanguageSource.IVisitor visitor) => visitor.Visit(this);
	}

}
