namespace Compiler
{
	public interface ILanguageSource
	{
		public interface IVisitor
		{
			void Visit(TopLevelInterfaceAndBodyPouLanguageSource topLevelInterfaceAndBodyPouLanguageSource);
			void Visit(GlobalVariableLanguageSource globalVariableLanguageSource);
			void Visit(DutLanguageSource dutLanguageSource);
		}
		void Accept(IVisitor visitor);
	}
}
