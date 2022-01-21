namespace Compiler
{
	public interface ILanguageSource
	{
		public string File { get; }
		public interface IVisitor
		{
			void Visit(TopLevelInterfaceAndBodyPouLanguageSource topLevelInterfaceAndBodyPouLanguageSource);
			void Visit(GlobalVariableListLanguageSource globalVariableLanguageSource);
			void Visit(DutLanguageSource dutLanguageSource);
			void Visit(TopLevelPouLanguageSource topLevelPouLanguageSource);
		}
		public interface IVisitor<T, TContext>
		{
			T Visit(TopLevelInterfaceAndBodyPouLanguageSource topLevelInterfaceAndBodyPouLanguageSource, TContext context);
			T Visit(GlobalVariableListLanguageSource globalVariableLanguageSource, TContext context);
			T Visit(DutLanguageSource dutLanguageSource, TContext context);
			T Visit(TopLevelPouLanguageSource topLevelPouLanguageSource, TContext context);
		}
		T Accept<T, TContext>(IVisitor<T, TContext> visitor, TContext context);
		void Accept(IVisitor visitor);
	}
}
