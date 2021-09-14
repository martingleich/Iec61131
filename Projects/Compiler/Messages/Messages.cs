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
					? $"Expected a {single.Name} but received a {ReceivedToken}."
					: $"Expected either a {MessageGrammarHelper.OrListing(ExpectedTokenTypes.Select(x => x.Name))} but received a {ReceivedToken}.";
	}
	public sealed class ExpectedExpressionMessage : ACriticalMessage
	{
		public ExpectedExpressionMessage(SourcePosition position) : base(position)
		{
		}

		public override string Text => "Expected a expression.";
	}

	public sealed class ConstantDoesNotFitIntoType : ACriticalMessage
	{
		public readonly ILiteralToken Token;
		public readonly IType TargetType;

		public ConstantDoesNotFitIntoType(ILiteralToken token, IType targetType) : base(token.SourcePosition)
		{
			Token = token ?? throw new ArgumentNullException(nameof(token));
			TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
		}

		public override string Text => $"The constant '{Token.Generating}' does not fit into the type {TargetType.Code}.";
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
		public readonly string Identifier;

		public TypeNotFoundMessage(string identifier, SourcePosition position) : base(position)
		{
			Identifier = identifier;
		}

		public override string Text => $"Cannot find a type named '{Identifier}'.";
	}
	public sealed class VariableNotFoundMessage : ACriticalMessage
	{
		public readonly string Identifier;

		public VariableNotFoundMessage(string identifier, SourcePosition position) : base(position)
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
		public override string Text => $"Cannot convert from {From} to {To}.";
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
}
