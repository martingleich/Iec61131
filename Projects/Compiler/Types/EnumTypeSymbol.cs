using Compiler.Messages;
using System;

namespace Compiler.Types
{
	public sealed class EnumTypeSymbol : ITypeSymbol, _IDelayedLayoutType, IScopeSymbol
	{
		public CaseInsensitiveString Name { get; }
		public string Code => Name.Original;
		public LayoutInfo LayoutInfo => BaseType.LayoutInfo;
		private IType? _baseType;
		public IType BaseType => _baseType ?? throw new InvalidOperationException("BaseType is not initialized yet");
		private SymbolSet<EnumVariableSymbol> _values;
		public SymbolSet<EnumVariableSymbol> Values => _values.IsDefault
			? throw new InvalidOperationException("Elements is not initialzed yet")
			: _values;

		public SourcePosition DeclaringPosition { get; }

		public EnumTypeSymbol(SourcePosition declaringPosition, CaseInsensitiveString name, IType baseType, SymbolSet<EnumVariableSymbol> values)
		{
			DeclaringPosition = declaringPosition;
			Name = name;
			_baseType = baseType ?? throw new ArgumentNullException(nameof(baseType));
			_values = values;
		}

		internal EnumTypeSymbol(SourcePosition declaringPosition, CaseInsensitiveString name)
		{
			DeclaringPosition = declaringPosition;
			Name = name;
		}
		internal void _SetBaseType(IType baseType)
		{
			_baseType = baseType ?? throw new ArgumentNullException(nameof(baseType));
		}
		internal void _SetValues(SymbolSet<EnumVariableSymbol> values)
		{
			_values = values;
		}

		public override string ToString() => Name.ToString();

		public UndefinedLayoutInfo GetLayoutInfo(MessageBag messageBag, SourcePosition position)
		{
			return DelayedLayoutType.GetLayoutInfo(BaseType, messageBag, position);
		}

		public void RecursiveLayout(MessageBag messageBag, SourcePosition position)
		{
			GetLayoutInfo(messageBag, position);
		}

		public void RecursiveInitializers(MessageBag messageBag)
		{
			foreach (var value in Values)
				value._GetConstantValue(messageBag);
		}
		public T Accept<T, TContext>(IType.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);

		public ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourcePosition errorPosition) => Values.TryGetValue(identifier, out var symbol)
			? ErrorsAnd.Create<IVariableSymbol>(symbol)
			: ErrorsAnd.Create(IVariableSymbol.CreateError(errorPosition, identifier), new EnumValueNotFoundMessage(this, identifier, errorPosition));
	}
}