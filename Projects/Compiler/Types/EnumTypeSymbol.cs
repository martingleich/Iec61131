using Compiler.Messages;
using System;

namespace Compiler.Types
{
	public sealed class EnumTypeSymbol : ITypeSymbol, _IDelayedLayoutType
	{
		public CaseInsensitiveString Name { get; }
		public string Code => Name.Original;
		public LayoutInfo LayoutInfo => BaseType.LayoutInfo;
		private IType? _baseType;
		public IType BaseType => _baseType ?? throw new InvalidOperationException("BaseType is not initialized yet");
		private SymbolSet<EnumValueSymbol> _values;
		public SymbolSet<EnumValueSymbol> Values => _values.IsDefault
			? throw new InvalidOperationException("Elements is not initialzed yet")
			: _values;

		public SourcePosition DeclaringPosition { get; }

		public EnumTypeSymbol(SourcePosition declaringPosition, CaseInsensitiveString name, IType baseType, SymbolSet<EnumValueSymbol> values)
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
		internal void _SetValues(SymbolSet<EnumValueSymbol> values)
		{
			_values = values;
		}

		public override string ToString() => Name.ToString();

		public LayoutInfo GetLayoutInfo(MessageBag messageBag, SourcePosition position)
		{
			return DelayedLayoutType.GetLayoutInfo(BaseType, messageBag, position);
		}

		public LayoutInfo RecursiveLayout(MessageBag messageBag, SourcePosition position)
		{
			return DelayedLayoutType.RecursiveLayout(BaseType, messageBag, position);
		}

		public void RecursiveInitializers(MessageBag messageBag, SourcePosition position)
		{
			foreach (var value in Values)
				value._GetConstantValue(messageBag, position);
		}
		public T Accept<T, TContext>(IType.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
	}
}