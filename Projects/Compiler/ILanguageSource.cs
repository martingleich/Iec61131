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
		}
		void Accept(IVisitor visitor);
	}
}
