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
	public sealed class CannotDereferenceTypeMessage : ACriticalMessage
	{
		public readonly IType Type;

		public CannotDereferenceTypeMessage(IType type, SourcePosition sourcePosition) : base(sourcePosition)
		{
			Type = type;
		}

		public override string Text => $"Cannot dereference a expression of type '{Type.Code}'.";
	}
	public sealed class CannotIndexTypeMessage : ACriticalMessage
	{
		public readonly IType Type;

		public CannotIndexTypeMessage(IType type, SourcePosition sourcePosition) : base(sourcePosition)
		{
			Type = type;
		}

		public override string Text => $"Cannot perform a index access on expression of type '{Type.Code}'.";
	}
	public sealed class CannotIndexWithTypeMessage : ACriticalMessage
	{
		public readonly IType Type;

		public CannotIndexWithTypeMessage(IType type, SourcePosition sourcePosition) : base(sourcePosition)
		{
			Type = type;
		}

		public override string Text => $"Cannot perform a index access with expression of type '{Type.Code}'.";
	}
	public sealed class WrongNumberOfDimensionInIndexMessage : ACriticalMessage
	{
		public readonly int ExpectedIndices;
		public readonly int PassedIndices;

		public WrongNumberOfDimensionInIndexMessage(int expectedIndices, int passedIndices, SourcePosition sourcePosition) : base(sourcePosition)
		{
			ExpectedIndices = expectedIndices;
			PassedIndices = passedIndices;
		}

		public override string Text => $"Expected {ExpectedIndices} indexes to access this array, but only received {PassedIndices}.";
	}
	public sealed class FieldNotFoundMessage : ACriticalMessage
	{
		public readonly IType BaseType;
		public readonly CaseInsensitiveString FieldName;

		public FieldNotFoundMessage(IType baseType, CaseInsensitiveString fieldName, SourcePosition sourcePosition) : base(sourcePosition)
		{
			BaseType = baseType ?? throw new ArgumentNullException(nameof(baseType));
			FieldName = fieldName;
		}

		public override string Text => $"The type '{BaseType.Code}' does not have a field '{FieldName}'.";
	}
	
	public sealed class OnlyVarGlobalInGvlMessages : ACriticalMessage
	{
		public OnlyVarGlobalInGvlMessages(SourcePosition sourcePosition) : base(sourcePosition)
		{
		}

		public override string Text => $"Only VAR_GLOBAL is allowed inside a GVL.";
	}

	public sealed class GlobalVariableNotFoundMessage : ACriticalMessage
	{
		public readonly GlobalVariableListSymbol Gvl;
		public readonly CaseInsensitiveString VarName;

		public GlobalVariableNotFoundMessage(GlobalVariableListSymbol gvl, CaseInsensitiveString varName, SourcePosition sourcePosition) : base(sourcePosition)
		{
			Gvl = gvl ?? throw new ArgumentNullException(nameof(gvl));
			VarName = varName;
		}

		public override string Text => $"The global variable list '{Gvl.Name}' does not have a variable '{VarName}'.";
	}
	public sealed class ExpectedVariableOrTypeOrGvlMessage : ACriticalMessage
	{
		public readonly CaseInsensitiveString Name;

		public static ExpectedVariableOrTypeOrGvlMessage Create(VariableExpressionSyntax expression)
			=> new (expression.Identifier, expression.SourcePosition);
		public ExpectedVariableOrTypeOrGvlMessage(CaseInsensitiveString name, SourcePosition sourcePosition) : base(sourcePosition)
		{
			Name = name;
		}

		public override string Text => $"Expected a variable, type or gvl name.";
	}
	public sealed class EnumValueNotFoundMessage : ACriticalMessage
	{
		public readonly EnumTypeSymbol EnumType;
		public readonly CaseInsensitiveString Name;

		public EnumValueNotFoundMessage(EnumTypeSymbol enumType, CaseInsensitiveString name, SourcePosition sourcePosition) : base(sourcePosition)
		{
			EnumType = enumType ?? throw new ArgumentNullException(nameof(enumType));
			Name = name;
		}

		public override string Text => $"The enumtype '{EnumType.Code}' does not contain a value named '{Name}'.";
	}
	public sealed class TypeDoesNotContainStaticVariableMessage : ACriticalMessage
	{
		public readonly IType Type;
		public readonly CaseInsensitiveString Name;

		public TypeDoesNotContainStaticVariableMessage(IType type, CaseInsensitiveString name, SourcePosition sourcePosition) : base(sourcePosition)
		{
			Type = type ?? throw new ArgumentNullException(nameof(type));
			Name = name;
		}

		public override string Text => $"The type '{Type.Code}' does not contain a static variable named '{Name}'.";
	}
}
