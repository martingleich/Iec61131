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

		void _IDelayedLayoutType.RecursiveLayout(MessageBag messageBag, SourceSpan span)
			=> DelayedLayoutType.RecursiveLayout(BaseType, messageBag, span);
		UndefinedLayoutInfo _IDelayedLayoutType.GetLayoutInfo(MessageBag messageBag, SourceSpan span)
			=> LayoutInfo;
		public T Accept<T, TContext>(IType.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
	}
}