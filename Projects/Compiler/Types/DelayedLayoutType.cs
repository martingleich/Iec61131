using Compiler.Messages;

namespace Compiler.Types
{
	public static class DelayedLayoutType
	{
		public static UndefinedLayoutInfo GetLayoutInfo(IType typeSymbol, MessageBag messageBag, SourcePosition position)
		{
			if (typeSymbol is _IDelayedLayoutType delayedLayoutType)
				return delayedLayoutType.GetLayoutInfo(messageBag, position);
			else
				return typeSymbol.LayoutInfo;
		}
		public static void RecursiveLayout(IType typeSymbol, MessageBag messageBag, SourcePosition position)
		{
			if (typeSymbol is _IDelayedLayoutType delayedLayoutType)
				delayedLayoutType.RecursiveLayout(messageBag, position);
		}
	}

	internal interface _IDelayedLayoutType
	{
		UndefinedLayoutInfo GetLayoutInfo(MessageBag messageBag, SourcePosition position);
		void RecursiveLayout(MessageBag messageBag, SourcePosition position);
	}

}