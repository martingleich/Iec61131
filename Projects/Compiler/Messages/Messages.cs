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
		public MissingEndOfMultilineCommentMessage(SourcePosition position, string expected) : base(position)
		{
			Expected = expected ?? throw new ArgumentNullException(nameof(expected));
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
			if (expectedTokenTypes is null) throw new ArgumentNullException(nameof(expectedTokenTypes));
			ReceivedToken = receivedToken ?? throw new ArgumentNullException(nameof(receivedToken));
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
	public sealed class ConstantDoesNotFitIntoTypeMessage : ACriticalMessage
	{
		public readonly string Generating;
		public readonly IType? TargetType;

		public ConstantDoesNotFitIntoTypeMessage(string generating, IType? targetType, SourcePosition sourcePosition) : base(sourcePosition)
		{
			Generating = generating ?? throw new ArgumentNullException(nameof(generating));
			TargetType = targetType;
		}

		public override string Text
		{
			get
			{
				if(TargetType == null)
					return $"There is not type that can contain the value '{Generating}'.";
				else
					return $"The value '{Generating}' is to large for the type '{TargetType.Code}'.";
			}
		}
	}
	public sealed class InvalidArrayRangesMessage : ACriticalMessage
	{
		public InvalidArrayRangesMessage(SourcePosition position) : base(position)
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
	public sealed class ScopeNotFoundMessage : ACriticalMessage
	{
		public readonly CaseInsensitiveString Identifier;

		public ScopeNotFoundMessage(CaseInsensitiveString identifier, SourcePosition position) : base(position)
		{
			Identifier = identifier;
		}

		public override string Text => $"Cannot find a scope named '{Identifier}'.";
	}
	public sealed class ParameterNotFoundMessage : ACriticalMessage
	{
		public readonly ICallableTypeSymbol Function;
		public readonly CaseInsensitiveString Identifier;

		public ParameterNotFoundMessage(ICallableTypeSymbol function, CaseInsensitiveString identifier, SourcePosition position) : base(position)
		{
			Function = function ?? throw new ArgumentNullException(nameof(function));
			Identifier = identifier;
		}

		public override string Text => $"Cannot find a parameter named '{Identifier}' in the function '{Function.Name}'.";
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
			From = from ?? throw new ArgumentNullException(nameof(from));
			To = to ?? throw new ArgumentNullException(nameof(to));
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
	public sealed class CannotAssignToVariableMessage : ACriticalMessage
	{
		public readonly IVariableSymbol Variable; 
		public CannotAssignToVariableMessage(IVariableSymbol variable, SourcePosition position) : base(position)
		{
			Variable = variable ?? throw new ArgumentNullException(nameof(variable));
		}

		public override string Text => $"Cannot assign to a new value to the variable {Variable.Name}.";
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
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public override string Text => $"Cannot dereference a expression of type '{Type.Code}'.";
	}
	public sealed class CannotIndexTypeMessage : ACriticalMessage
	{
		public readonly IType Type;

		public CannotIndexTypeMessage(IType type, SourcePosition sourcePosition) : base(sourcePosition)
		{
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public override string Text => $"Cannot perform a index access on expression of type '{Type.Code}'.";
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

	public sealed class OnlyVarGlobalInGvlMessage : ACriticalMessage
	{
		public OnlyVarGlobalInGvlMessage(SourcePosition sourcePosition) : base(sourcePosition)
		{
		}

		public override string Text => $"Only VAR_GLOBAL is allowed inside a GVL.";
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

	public sealed class TypeExpectedMessage : ACriticalMessage
	{
		public readonly IToken ReceivedToken;
		public TypeExpectedMessage(IToken receivedToken) : base(receivedToken.SourcePosition)
		{
			ReceivedToken = receivedToken ?? throw new ArgumentNullException(nameof(receivedToken));
		}

		public override string Text => $"Expected a type but found '{ReceivedToken.Generating}' instead.";
	}
	public sealed class CannotCallTypeMessage : ACriticalMessage
	{
		public readonly IType CalledType;

		public CannotCallTypeMessage(IType calledType, SourcePosition sourcePosition) : base(sourcePosition)
		{
			CalledType = calledType ?? throw new ArgumentNullException(nameof(calledType));
		}

		public override string Text => $"The type '{CalledType.Code}' is not callable.";
	}
	public sealed class WrongNumberOfArgumentsMessage : ACriticalMessage
	{
		public readonly ICallableTypeSymbol Function;
		public readonly int PassedCount;

		public WrongNumberOfArgumentsMessage(ICallableTypeSymbol function, int passedCount, SourcePosition sourcePosition) : base(sourcePosition)
		{
			Function = function ?? throw new ArgumentNullException(nameof(function));
			PassedCount = passedCount;
		}

		public override string Text => $"The function '{Function.Name}' takes {Function.GetParameterCountWithoutReturn()} arguments, but {PassedCount} arguments were passed.";
	}
	public sealed class NonInputParameterMustBePassedExplicit : ACriticalMessage
	{
		public readonly ParameterVariableSymbol Symbol;

		public NonInputParameterMustBePassedExplicit(ParameterVariableSymbol symbol, SourcePosition sourcePosition) : base(sourcePosition)
		{
			Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
		}

		public override string Text => $"The parameter '{Symbol.Name}' is of type '{Symbol.Kind.Code}' and must be passed explicit.";
	}
	
	public sealed class InoutArgumentMustHaveSameTypeMessage : ACriticalMessage
	{
		public readonly IType Type1;
		public readonly IType Type2;

		public InoutArgumentMustHaveSameTypeMessage(IType type1, IType type2, SourcePosition sourcePosition) : base(sourcePosition)
		{
			Type1 = type1 ?? throw new ArgumentNullException(nameof(type1));
			Type2 = type2 ?? throw new ArgumentNullException(nameof(type2));
		}

		public override string Text => $"The argument type '{Type1}' and the type of the inout '{Type2}' must be identical.";
	}
	
	public sealed class ParameterKindDoesNotMatchAssignMessage : ACriticalMessage
	{
		public readonly ParameterVariableSymbol Symbol;
		public readonly IParameterKindToken ParameterKind;

		public ParameterKindDoesNotMatchAssignMessage(ParameterVariableSymbol symbol, IParameterKindToken parameterKind) : base(parameterKind.SourcePosition)
		{
			Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
			ParameterKind = parameterKind ?? throw new ArgumentNullException(nameof(parameterKind));
		}

		public override string Text => $"The argument for a '{Symbol.Kind.Code}' parameter must be passed with the '{Symbol.Kind.AssignCode}'-Assignoperator.";
	}
	
	public sealed class ParameterWasAlreadyPassedMessage : ACriticalMessage
	{
		public readonly ParameterVariableSymbol Symbol;
		public readonly SourcePosition OriginalPosition;

		public ParameterWasAlreadyPassedMessage(ParameterVariableSymbol symbol, SourcePosition originalPosition, SourcePosition duplicatePosition) : base(duplicatePosition)
		{
			Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
			OriginalPosition = originalPosition;
		}

		public override string Text => $"The parameter '{Symbol.Name}' was already passed earlier.";
	}
	public sealed class CannotUsePositionalParameterAfterExplicitMessage : ACriticalMessage
	{
		public CannotUsePositionalParameterAfterExplicitMessage(SourcePosition position) : base(position)
		{
		}

		public override string Text => $"Cannot use a positional parameter after explicit ones.";
	}
	
	public sealed class CannotUseTypeAsLoopIndexMessage : ACriticalMessage
	{
		public readonly IType Type;
		public CannotUseTypeAsLoopIndexMessage(IType type, SourcePosition position) : base(position)
		{
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public override string Text => $"Cannot use the type '{Type.Code}' as index in a for loop. The type must support addition.";
	}
	public sealed class UnknownDurationUnitMessage : ACriticalMessage
	{
		public readonly CaseInsensitiveString UnitText;
		public UnknownDurationUnitMessage(CaseInsensitiveString unitText, SourcePosition position) : base(position)
		{
			UnitText = unitText; 
		}

		public override string Text
		{
			get
			{
				if (UnitText.Length > 0)
					return $"Unknown unit for a duration '{UnitText}'";
				else
					return "Missing unit for duration";
			}
		}
	}
	public sealed class VariableCannotHaveInitialValueMessage : ACriticalMessage
	{
		public VariableCannotHaveInitialValueMessage(SourcePosition position) : base(position)
		{
		}

		public override string Text => "This variable cannot have an initial value.";
	}
	
	public sealed class CannotInferTypeForInitializerMessage : ACriticalMessage
	{
		public CannotInferTypeForInitializerMessage(SourcePosition position) : base(position)
		{
		}

		public override string Text => "Cannot infer a type for this structured initializer.";
	}
	public sealed class CannotUseAnInitializerForThisTypeMessage : ACriticalMessage
	{
		public CannotUseAnInitializerForThisTypeMessage(SourcePosition position) : base(position)
		{
		}

		public override string Text => "Cannot use an initializer for this type.";
	}
	public sealed class TypeDoesNotHaveThisElementMessage : ACriticalMessage
	{
		public TypeDoesNotHaveThisElementMessage(SourcePosition position) : base(position)
		{
		}

		public override string Text => "This type does not have this element.";
	}
	public sealed class DuplicateInitializerElementMessage : ACriticalMessage
	{
		public readonly SourcePosition Original;
		public DuplicateInitializerElementMessage(SourcePosition position, SourcePosition original) : base(position)
		{
			Original = original;
		}

		public override string Text => "Element was already initialzed earlier.";
	}
	public sealed class CannotUsePositionalElementAfterExplicitMessage : ACriticalMessage
	{
		public CannotUsePositionalElementAfterExplicitMessage(SourcePosition position) : base(position)
		{
		}

		public override string Text => "Cannot use an implicit element after an explicit on.";
	}
	public sealed class IndexNotInitializedMessage : ACriticalMessage
	{
		public readonly int Index;
		public IndexNotInitializedMessage(int index, SourcePosition position) : base(position)
		{
			Index = index;
		}

		public override string Text => $"Missing initializer for index '{Index}'.";
	}
	public sealed class FieldNotInitializedMessage : ACriticalMessage
	{
		public readonly FieldVariableSymbol Field;
		public FieldNotInitializedMessage(FieldVariableSymbol field, SourcePosition position) : base(position)
		{
			Field = field ?? throw new ArgumentNullException(nameof(field));
		}

		public override string Text => $"Missing initializer for field '{Field.Name}'.";
	}
	
	public sealed class MissingElementsInInitializerMessage : ACriticalMessage
	{
		public MissingElementsInInitializerMessage(SourcePosition position) : base(position)
		{
		}

		public override string Text => $"Missing initializer.";
	}
	public sealed class CannotUseImplicitInitializerForThisTypeMessage : ACriticalMessage
	{
		public CannotUseImplicitInitializerForThisTypeMessage(SourcePosition position) : base(position)
		{
		}

		public override string Text => $"Cannot use implicit initializer for this type.";
	}
}
