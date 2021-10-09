using Compiler.Messages;
using Compiler.Types;

namespace Compiler.Scopes
{
	public sealed class VariableSetScope : AInnerScope<IScope>
	{
		public VariableSetScope(SymbolSet<LocalVariableSymbol> variables, IScope outerScope) : base(outerScope)
		{
			Variables = variables;
		}
		public SymbolSet<LocalVariableSymbol> Variables { get; }

		public override ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourcePosition sourcePosition)
			=> Variables.TryGetValue(identifier, out var value)
			? ErrorsAnd.Create<IVariableSymbol>(value)
			: base.LookupVariable(identifier, sourcePosition);
	}
}
