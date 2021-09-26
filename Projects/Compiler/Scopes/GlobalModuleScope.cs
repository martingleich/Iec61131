using System;
using Compiler.Messages;
using Compiler.Types;

namespace Compiler.Scopes
{
	public sealed class GlobalModuleScope : AInnerScope<IScope>
	{
		private readonly BoundModuleInterface Interface;

		public GlobalModuleScope(BoundModuleInterface @interface, IScope inner) : base(inner)
		{
			Interface = @interface ?? throw new ArgumentNullException(nameof(@interface));
		}

		public override ErrorsAnd<ITypeSymbol> LookupType(CaseInsensitiveString identifier, SourcePosition sourcePosition) => Interface.DutTypes.TryGetValue(identifier, out var dutType)
			? ErrorsAnd.Create(dutType)
			: base.LookupType(identifier, sourcePosition);
	}
}
