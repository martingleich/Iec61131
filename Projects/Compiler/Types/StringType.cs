using Compiler.Messages;
using Compiler.Scopes;
using System;

namespace Compiler.Types
{
	public sealed class StringType : IType, _IDelayedLayoutType
	{
		private readonly StringTypeSyntax? MaybeSyntax;
		private readonly IScope? MaybeScope;

		private int? MaybeSize;
		public string Code => $"STRING[{Size}]";
		public StringType(IScope scope, StringTypeSyntax syntax)
		{
			MaybeScope = scope ?? throw new ArgumentNullException(nameof(scope));
			MaybeSyntax = syntax ?? throw new ArgumentNullException(nameof(syntax));
		}
		public StringType(int size)
		{
			MaybeSize = size;
		}
		public int Size => MaybeSize!.Value;
		public LayoutInfo LayoutInfo => new(Size, 1);
		public LayoutInfo? MaybeLayoutInfo => MaybeSize is int size ? new LayoutInfo(size, 1) : null;

		public override string ToString() => Code;

		void _IDelayedLayoutType.RecursiveLayout(MessageBag messageBag, SourcePosition position)
		{
			((_IDelayedLayoutType)this).GetLayoutInfo(messageBag, position);
		}
		private static int CalculateStringSize(IScope scope, MessageBag messageBag, IExpressionSyntax? sizeExpr)
		{
			if (sizeExpr == null)
				return 80;
			else
			{
				var boundSizeExpr = ExpressionBinder.Bind(sizeExpr, scope, messageBag, scope.SystemScope.DInt);
				var sizeValue = ConstantExpressionEvaluator.EvaluateConstant(boundSizeExpr, messageBag, scope.SystemScope);
				if (sizeValue is DIntLiteralValue dintLiteralValue)
					return dintLiteralValue.Value;
				else
					return 0;
			}
		}
		UndefinedLayoutInfo _IDelayedLayoutType.GetLayoutInfo(MessageBag messageBag, SourcePosition position)
		{
			if (!MaybeSize.HasValue)
				MaybeSize = CalculateStringSize(MaybeScope!, messageBag, MaybeSyntax!.Size?.Size);
			return LayoutInfo;
		}
		public T Accept<T, TContext>(IType.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
	}
}
