using Compiler.Messages;
using System;

namespace Compiler.Types
{
	public sealed class PointerType : IType, _IDelayedLayoutType
	{
		public string Code => $"POINTER TO {BaseType.Code}";
		public readonly IType BaseType;

		public PointerType(IType baseType)
		{
			BaseType = baseType ?? throw new ArgumentNullException(nameof(baseType));
		}

		public LayoutInfo LayoutInfo => new(4, 4);

		public override string ToString() => Code;

		LayoutInfo _IDelayedLayoutType.RecursiveLayout(MessageBag messageBag, SourcePosition position)
			=> DelayedLayoutType.RecursiveLayout(BaseType, messageBag, position);
		LayoutInfo _IDelayedLayoutType.GetLayoutInfo(MessageBag messageBag, SourcePosition position)
			=> LayoutInfo;
		public T Accept<T, TContext>(IType.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
	}
}