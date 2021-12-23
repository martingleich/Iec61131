using Compiler.Messages;
using System;

namespace Compiler.Types
{
	public sealed class EnumTypeSymbol : ITypeSymbol, _IDelayedLayoutType, IScopeSymbol
	{
		public CaseInsensitiveString Name => UniqueId.Name;
		public UniqueSymbolId UniqueId { get; }
		public string Code => Name.Original;
		public LayoutInfo LayoutInfo => BaseType.LayoutInfo;
		private IType? _baseType;
		public IType BaseType => _baseType ?? throw new InvalidOperationException("BaseType is not initialized yet");
		private SymbolSet<EnumVariableSymbol> _values;
		public SymbolSet<EnumVariableSymbol> Values => _values.IsDefault
			? throw new InvalidOperationException("Elements is not initialzed yet")
			: _values;

		public SourceSpan DeclaringSpan { get; }

		public EnumTypeSymbol(SourceSpan declaringSpan, CaseInsensitiveString module, CaseInsensitiveString name, IType baseType, SymbolSet<EnumVariableSymbol> values)
		{
			DeclaringSpan = declaringSpan;
			_baseType = baseType ?? throw new ArgumentNullException(nameof(baseType));
			_values = values;
			UniqueId = new UniqueSymbolId(module, name);
		}

		internal EnumTypeSymbol(SourceSpan declaringSpan, CaseInsensitiveString module, CaseInsensitiveString name)
		{
			DeclaringSpan = declaringSpan;
			UniqueId = new UniqueSymbolId(module, name);
		}
		internal void _SetBaseType(IType baseType)
		{
			_baseType = baseType ?? throw new ArgumentNullException(nameof(baseType));
		}
		internal void _SetValues(SymbolSet<EnumVariableSymbol> values)
		{
			_values = values;
		}

		public override string ToString() => UniqueId.ToString();

		public UndefinedLayoutInfo GetLayoutInfo(MessageBag messageBag, SourceSpan span)
		{
			return DelayedLayoutType.GetLayoutInfo(BaseType, messageBag, span);
		}

		public void RecursiveLayout(MessageBag messageBag, SourceSpan span)
		{
			GetLayoutInfo(messageBag, span);
		}

		public void RecursiveInitializers(MessageBag messageBag)
		{
			foreach (var value in Values)
				value._GetConstantValue(messageBag);
		}
		public T Accept<T, TContext>(IType.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);

		public ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourceSpan errorPosition) => Values.TryGetValue(identifier, out var symbol)
			? ErrorsAnd.Create<IVariableSymbol>(symbol)
			: ErrorsAnd.Create(IVariableSymbol.CreateError(errorPosition, identifier), new EnumValueNotFoundMessage(this, identifier, errorPosition));
		public ErrorsAnd<IScopeSymbol> LookupScope(CaseInsensitiveString identifier, SourceSpan errorPosition)
			=> EmptyScopeHelper.LookupScope(Name, identifier, errorPosition);
		public ErrorsAnd<ITypeSymbol> LookupType(CaseInsensitiveString identifier, SourceSpan errorPosition)
			=> EmptyScopeHelper.LookupType(Name, identifier, errorPosition);
	}
}