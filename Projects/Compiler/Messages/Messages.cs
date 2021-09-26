using Compiler.Types;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Compiler.Messages
{
	public interface IMessage
	{
		SourcePosition Position { get; }
		string Text { get; }
		bool Critical { get; }
	}
	public abstract class ACriticalMessage : IMessage
	{
		protected ACriticalMessage(SourcePosition position)
		{
			Position = position;
		}

		public SourcePosition Position { get; }
		public abstract string Text { get; }
		public bool Critical => true;
		[ExcludeFromCodeCoverage]
		public override string ToString() => $"{Position} {Text}";
	}
	public abstract class AUncriticalMessage : IMessage
	{
		protected AUncriticalMessage(SourcePosition position)
		{
			Position = position;
		}

		public SourcePosition Position { get; }
		public abstract string Text { get; }
		public bool Critical => false;
		[ExcludeFromCodeCoverage]
		public override string ToString() => $"{Position} {Text}";
	}
	public sealed class InvalidBooleanLiteralMessage : ACriticalMessage
	{
		public InvalidBooleanLiteralMessage(SourcePosition position) : base(position)
		{
		}

		public override string Text => "Expected '0','1','TRUE' or 'FALSE'";
	}
	public sealed class MissingEndOfMultilineCommentMessage : ACriticalMessage
	{
		public readonly string Expected;
		public MissingEndOfMultilineCommentMessage( SourcePosition position, string expected) : base(position)
		{
			Expected = expected;
		}

		public override string Text => $"Could not find the string '{Expected}' terminating the multiline comment.";
	}
	public sealed class MissingEndOfAttributeMessage : ACriticalMessage
	{
		public MissingEndOfAttributeMessage(SourcePosition position) : base(position)
		{
		}

		public override string Text => "Could not find the string '}' terminating the attribute.";
	}
	public sealed class UnexpectedTokenMessage : ACriticalMessage
	{
		public UnexpectedTokenMessage(IToken receivedToken, params Type[] expectedTokenTypes) : base(receivedToken.SourcePosition)
		{
			ReceivedToken = receivedToken;
			ExpectedTokenTypes = expectedTokenTypes.ToImmutableArray();
		}
		public IToken ReceivedToken { get; }
		public ImmutableArray<Type> ExpectedTokenTypes { get; }
		public override string Text => ExpectedTokenTypes.TryGetSingle(out var single)
					? $"Expected a {single.Name} but received '{ReceivedToken.Generating}'."
					: $"Expected either a {MessageGrammarHelper.OrListing(ExpectedTokenTypes.Select(x => x.Name))} but received '{ReceivedToken.Generating}'.";
	}
	public sealed class ExpectedExpressionMessage : ACriticalMessage
	{
		public ExpectedExpressionMessage(SourcePosition position) : base(position)
		{
		}

		public override string Text => "Expected a expression.";
	}
	public sealed class IntegerIsToLargeForTypeMessage : ACriticalMessage
	{
		public readonly OverflowingInteger Value;
		public readonly IType TargetType;

		public IntegerIsToLargeForTypeMessage(OverflowingInteger value, IType targetType, SourcePosition sourcePosition) : base(sourcePosition)
		{
			Value = value;
			TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
		}

		public override string Text => $"The constant '{Value}' does not fit into the type {TargetType.Code}.";
	}
	public sealed class RealIsToLargeForTypeMessage : ACriticalMessage
	{
		public readonly OverflowingReal Value;
		public readonly IType TargetType;

		public RealIsToLargeForTypeMessage(OverflowingReal value, IType targetType, SourcePosition sourcePosition) : base(sourcePosition)
		{
			Value = value;
			TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
		}

		public override string Text => $"The constant '{Value}' does not fit into the type {TargetType.Code}.";
	}
	public sealed class ConstantDoesNotFitIntoAnyType : ACriticalMessage
	{
		public readonly ILiteralToken Token;

		public ConstantDoesNotFitIntoAnyType(ILiteralToken token) : base(token.SourcePosition)
		{
			Token = token ?? throw new ArgumentNullException(nameof(token));
		}

		public override string Text => $"There is not type that can contain the value '{Token.Generating}'";
	}
	public sealed class InvalidArrayRangesMessages : ACriticalMessage
	{
		public InvalidArrayRangesMessages(SourcePosition position) : base(position)
		{
		}
		public override string Text => $"The array ranges are invalid";
	}
	public sealed class TypeNotFoundMessage : ACriticalMessage
	{
		public readonly CaseInsensitiveString Identifier;

		public TypeNotFoundMessage(CaseInsensitiveString identifier, SourcePosition position) : base(position)
		{
			Identifier = identifier;
		}

		public override string Text => $"Cannot find a type named '{Identifier}'.";
	}
	public sealed class VariableNotFoundMessage : ACriticalMessage
	{
		public readonly CaseInsensitiveString Identifier;

		public VariableNotFoundMessage(CaseInsensitiveString identifier, SourcePosition position) : base(position)
		{
			Identifier = identifier;
		}

		public override string Text => $"Cannot find a variable named '{Identifier}'.";
	}
	public sealed class TypeNotCompleteMessage : ACriticalMessage
	{
		public TypeNotCompleteMessage(SourcePosition position) : base(position)
		{
		}

		public override string Text => $"Type not complete yet.";
	}
	public sealed class SymbolAlreadyExistsMessage : ACriticalMessage
	{
		public readonly CaseInsensitiveString Name;
		public readonly SourcePosition AlreadyDeclaredPosition;
		public SymbolAlreadyExistsMessage(CaseInsensitiveString name, SourcePosition first, SourcePosition second) : base(second)
		{
			Name = name;
			AlreadyDeclaredPosition = first;
		}
		public override string Text => $"The symbol '{Name}' was already declared earlier.";
	}
	public sealed class TypeIsNotConvertibleMessage : ACriticalMessage
	{
		public readonly IType From;
		public readonly IType To;

		public TypeIsNotConvertibleMessage(IType from, IType to, SourcePosition position) : base(position)
		{
			From = from;
			To = to;
		}
		public override string Text => $"Cannot convert from {From.Code} to {To.Code}.";
	}
	public sealed class NotAConstantMessage : ACriticalMessage
	{
		public NotAConstantMessage(SourcePosition position) : base(position)
		{
		}
		public override string Text => $"Not a constant value.";
	}
	public sealed class RecursiveConstantDeclarationMessage : ACriticalMessage
	{
		public RecursiveConstantDeclarationMessage(SourcePosition position) : base(position)
		{
		}
		public override string Text => $"Recursive constant declaration";
	}
	public sealed class CannotPerformArithmeticOnTypesMessage : ACriticalMessage
	{
		private readonly ImmutableArray<IType> Types;

		public CannotPerformArithmeticOnTypesMessage(SourcePosition position, params IType[] types) : base(position)
		{
			this.Types = types.ToImmutableArray();
		}

		public override string Text
		{
			get
			{
				var list = MessageGrammarHelper.AndListing(Types.Select(t => $"'{t.Code}'"));
				return $"Cannot perform this arithmetic operation on the types {list}.";
			}
		}
	}
	public sealed class CannotAssignToSyntaxMessage : ACriticalMessage
	{
		public CannotAssignToSyntaxMessage(SourcePosition position) : base(position)
		{
		}

		public override string Text => $"Cannot assign to this syntax.";
	}
	public sealed class ConstantValueIsToLargeForTargetMessage : ACriticalMessage
	{
		public readonly OverflowingInteger Value;
		public readonly IType Type;

		public ConstantValueIsToLargeForTargetMessage(OverflowingInteger value, IType type, SourcePosition position) : base(position)
		{
			Value = value;
			Type = type;
		}
		public override string Text => $"The value '{Value}' is to large for the type '{Type.Code}'.";
	}

	public sealed class SyntaxOnlyAllowedInLoopMessage : ACriticalMessage
	{
		public SyntaxOnlyAllowedInLoopMessage(SourcePosition position) : base(position)
		{
		}
		public override string Text => $"This syntax is not allowed outside of a loop.";
	}
	public sealed class OverflowInConstantContextMessage : ACriticalMessage
	{
		public OverflowInConstantContextMessage(SourcePosition sourcePosition) : base(sourcePosition)
		{
		}

		public override string Text => $"Overflow in constant context.";
	}
	public sealed class DivsionByZeroInConstantContextMessage : ACriticalMessage
	{
		public DivsionByZeroInConstantContextMessage(SourcePosition sourcePosition) : base(sourcePosition)
		{
		}

		public override string Text => $"Division by zero in constant context.";
	}
}
