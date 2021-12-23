using Compiler.Messages;

namespace Compiler.Types
{
	public static class DelayedLayoutType
	{
		public static UndefinedLayoutInfo GetLayoutInfo(IType typeSymbol, MessageBag messageBag, SourceSpan span)
		{
			if (typeSymbol is _IDelayedLayoutType delayedLayoutType)
				return delayedLayoutType.GetLayoutInfo(messageBag, span);
			else
				return typeSymbol.LayoutInfo;
		}
		public static void RecursiveLayout(IType typeSymbol, MessageBag messageBag, SourceSpan span)
		{
			if (typeSymbol is _IDelayedLayoutType delayedLayoutType)
				delayedLayoutType.RecursiveLayout(messageBag, span);
		}
	}

	internal interface _IDelayedLayoutType
	{
		UndefinedLayoutInfo GetLayoutInfo(MessageBag messageBag, SourceSpan span);
		void RecursiveLayout(MessageBag messageBag, SourceSpan span);
	}

}