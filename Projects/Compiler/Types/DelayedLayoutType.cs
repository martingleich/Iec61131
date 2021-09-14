using Compiler.Messages;

namespace Compiler.Types
{
	public static class DelayedLayoutType
	{
		public static LayoutInfo GetLayoutInfo(IType typeSymbol, MessageBag messageBag, SourcePosition position)
		{
			if (typeSymbol is _IDelayedLayoutType delayedLayoutType)
				return delayedLayoutType.GetLayoutInfo(messageBag, position);
			else
				return typeSymbol.LayoutInfo;
		}
		public static LayoutInfo RecursiveLayout(IType typeSymbol, MessageBag messageBag, SourcePosition position)
		{
			if (typeSymbol is _IDelayedLayoutType delayedLayoutType)
				return delayedLayoutType.RecursiveLayout(messageBag, position);
			else
				return typeSymbol.LayoutInfo;
		}
	}

	internal interface _IDelayedLayoutType
	{
		LayoutInfo GetLayoutInfo(MessageBag messageBag, SourcePosition position);
		LayoutInfo RecursiveLayout(MessageBag messageBag, SourcePosition position);
	}
	public static class TypeExtensions
	{
		public static bool IsError(this IType self) => self is ErrorTypeSymbol;
	}

}