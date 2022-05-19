using Compiler.Types;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Compiler.Messages
{
    public interface IMessage
	{
		SourceSpan Span { get; }
		string Text { get; }
		bool Critical { get; }
		string ToString(IMessageFormatter formatter);
	}
    public abstract class AMessage : IMessage
    {
        protected AMessage(SourceSpan span)
        {
			Span = span;
        }
		public SourceSpan Span { get; }
		public abstract string Text { get; }
		public abstract bool Critical { get; }
		[ExcludeFromCodeCoverage]
		public string ToString(IMessageFormatter formatter)
		{
            if (formatter is null)
                throw new ArgumentNullException(nameof(formatter));

            var spanName = formatter.GetSourceName(Span);
			var kindName = formatter.GetKindName(Critical);
			return $"{kindName}@{spanName}: {Text}";
		}
		[ExcludeFromCodeCoverage]
		public override string ToString() => ToString(MessageFormatter.Null);
    }
    public abstract class ACriticalMessage : AMessage
	{
		protected ACriticalMessage(SourceSpan span) : base(span) { }
		public override bool Critical => true;
	}
	public abstract class AUncriticalMessage : AMessage
	{
		protected AUncriticalMessage(SourceSpan span) : base(span) { }
		public override bool Critical => false;
	}
	public sealed class InvalidBooleanLiteralMessage : ACriticalMessage
	{
		public InvalidBooleanLiteralMessage(SourceSpan span) : base(span)
		{
		}

		public override string Text => "Expected '0','1','TRUE' or 'FALSE'";
	}
	public sealed class MissingEndOfMultilineCommentMessage : ACriticalMessage
	{
		public readonly string Expected;
		public MissingEndOfMultilineCommentMessage(SourceSpan span, string expected) : base(span)
		{
			Expected = expected ?? throw new ArgumentNullException(nameof(expected));
		}

		public override string Text => $"Could not find the '{Expected}' terminating the multiline comment.";
	}
	public sealed class MissingEndOfAttributeMessage : ACriticalMessage
	{
		public MissingEndOfAttributeMessage(SourceSpan span) : base(span)
		{
		}

		public override string Text => "Could not find the '}' terminating the attribute.";
	}

	public sealed class ExpectedSyntaxMessage : ACriticalMessage
	{
		private ExpectedSyntaxMessage(IToken receivedToken, bool exact, string expected) : base(receivedToken.SourceSpan)
		{
			ReceivedToken = receivedToken ?? throw new ArgumentNullException(nameof(receivedToken));
			Expected = expected;
			Exact = exact;
		}
		public static ExpectedSyntaxMessage Token<T>(IToken receivedToken) where T : IToken
			=> new(receivedToken, true, GetTokenDesc(typeof(T)));
		public static ExpectedSyntaxMessage CommaOrEnd<TEnd>(IToken receivedToken) where TEnd:IToken
			=> new(receivedToken, false, $"'{GetTokenDesc(typeof(CommaToken))}' or '{GetTokenDesc(typeof(TEnd))}'");
		public static ExpectedSyntaxMessage Expression(IToken receivedToken)
			=> new(receivedToken, true, "<Expression>");
		public static ExpectedSyntaxMessage Type(IToken receivedToken)
			=> new(receivedToken, true, "<Type>");
		public static ExpectedSyntaxMessage Statement(IToken receivedToken)
			=> new(receivedToken, true, "<Statement>");

		private IToken ReceivedToken { get; }
		private string Expected { get; }
		private bool Exact { get; }
		private static string GetTokenDesc(Type tokenType)
		{
			if (tokenType == typeof(IPouKindToken))
				return "<PouKind>";
			if (tokenType == typeof(IVarDeclKindToken))
				return "<VarKind>";
			if (tokenType == typeof(IdentifierToken))
				return "<Identifier>";
			var generating = ScannerKeywordTable.GetDefaultGenerating(tokenType);
			if (generating != null)
				return $"{generating}";
			else
				return $"<{tokenType.Name}>";
		}
		public override string Text
		{
			get
			{
				var received = ReceivedToken is EndToken
					? "end-of-file"
					: $"'{ReceivedToken.Generating}'";
				var expected = Exact
					? $"'{Expected}'"
					: Expected;
				return $"Unexpected {received}, expected {expected}.";
			}
		}
		public static ExpectedSyntaxMessage? TryConcat(ExpectedSyntaxMessage a, ExpectedSyntaxMessage b)
		{
			if (ReferenceEquals(a.ReceivedToken, b.ReceivedToken) && a.Exact && b.Exact)
				return new ExpectedSyntaxMessage(a.ReceivedToken, true, a.Expected + " " + b.Expected);
			else
				return null;
		}
	}
	public sealed class ConstantDoesNotFitIntoTypeMessage : ACriticalMessage
	{
		public readonly string Generating;
		public readonly IType? TargetType;

		public ConstantDoesNotFitIntoTypeMessage(string generating, IType? targetType, SourceSpan sourceSpan) : base(sourceSpan)
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
		public InvalidArrayRangesMessage(SourceSpan span) : base(span)
		{
		}
		public override string Text => $"The array ranges are invalid";
	}
	public sealed class TypeNotFoundMessage : ACriticalMessage
	{
		public readonly CaseInsensitiveString Identifier;

		public TypeNotFoundMessage(CaseInsensitiveString identifier, SourceSpan span) : base(span)
		{
			Identifier = identifier;
		}

		public override string Text => $"Cannot find a type named '{Identifier}'.";
	}
	public sealed class VariableNotFoundMessage : ACriticalMessage
	{
		public readonly CaseInsensitiveString Identifier;

		public VariableNotFoundMessage(CaseInsensitiveString identifier, SourceSpan span) : base(span)
		{
			Identifier = identifier;
		}

		public override string Text => $"Cannot find a variable named '{Identifier}'.";
	}
	public sealed class ScopeNotFoundMessage : ACriticalMessage
	{
		public readonly CaseInsensitiveString Identifier;

		public ScopeNotFoundMessage(CaseInsensitiveString identifier, SourceSpan span) : base(span)
		{
			Identifier = identifier;
		}

		public override string Text => $"Cannot find a scope named '{Identifier}'.";
	}
	public sealed class ParameterNotFoundMessage : ACriticalMessage
	{
		public readonly ICallableTypeSymbol Function;
		public readonly CaseInsensitiveString Identifier;

		public ParameterNotFoundMessage(ICallableTypeSymbol function, CaseInsensitiveString identifier, SourceSpan span) : base(span)
		{
			Function = function ?? throw new ArgumentNullException(nameof(function));
			Identifier = identifier;
		}

		public override string Text => $"Cannot find a parameter named '{Identifier}' in the function '{Function.Name}'.";
	}
	public sealed class TypeNotCompleteMessage : ACriticalMessage
	{
		public TypeNotCompleteMessage(SourceSpan span) : base(span)
		{
		}

		public override string Text => $"Type not complete yet.";
	}
	public sealed class SymbolAlreadyExistsMessage : ACriticalMessage
	{
		public readonly CaseInsensitiveString Name;
		public readonly SourceSpan AlreadyDeclaredPosition;
		public SymbolAlreadyExistsMessage(CaseInsensitiveString name, SourceSpan first, SourceSpan second) : base(second)
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

		public TypeIsNotConvertibleMessage(IType from, IType to, SourceSpan span) : base(span)
		{
			From = from ?? throw new ArgumentNullException(nameof(from));
			To = to ?? throw new ArgumentNullException(nameof(to));
		}
		public override string Text => $"Cannot convert from {From.Code} to {To.Code}.";
	}
	public sealed class NotAConstantMessage : ACriticalMessage
	{
		public NotAConstantMessage(SourceSpan span) : base(span)
		{
		}
		public override string Text => $"Not a constant value.";
	}
	public sealed class RecursiveConstantDeclarationMessage : ACriticalMessage
	{
		public RecursiveConstantDeclarationMessage(SourceSpan span) : base(span)
		{
		}
		public override string Text => $"Recursive constant declaration";
	}
	public sealed class CannotPerformArithmeticOnTypesMessage : ACriticalMessage
	{
		private readonly ImmutableArray<IType> Types;

		public CannotPerformArithmeticOnTypesMessage(SourceSpan span, params IType[] types) : base(span)
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
		public CannotAssignToSyntaxMessage(SourceSpan span) : base(span)
		{
		}

		public override string Text => $"Cannot assign to this syntax.";
	}
	public sealed class CannotAssignToVariableMessage : ACriticalMessage
	{
		public readonly IVariableSymbol Variable; 
		public CannotAssignToVariableMessage(IVariableSymbol variable, SourceSpan span) : base(span)
		{
			Variable = variable ?? throw new ArgumentNullException(nameof(variable));
		}

		public override string Text => $"Cannot assign a new value to the variable {Variable.Name}.";
	}
	public sealed class SyntaxOnlyAllowedInLoopMessage : ACriticalMessage
	{
		public SyntaxOnlyAllowedInLoopMessage(SourceSpan span) : base(span)
		{
		}
		public override string Text => $"This syntax is not allowed outside of a loop.";
	}
	public sealed class OverflowInConstantContextMessage : ACriticalMessage
	{
		public OverflowInConstantContextMessage(SourceSpan sourceSpan) : base(sourceSpan)
		{
		}

		public override string Text => $"Overflow in constant context.";
	}
	public sealed class DivsionByZeroInConstantContextMessage : ACriticalMessage
	{
		public DivsionByZeroInConstantContextMessage(SourceSpan sourceSpan) : base(sourceSpan)
		{
		}

		public override string Text => $"Division by zero in constant context.";
	}
	public sealed class CannotDereferenceTypeMessage : ACriticalMessage
	{
		public readonly IType Type;

		public CannotDereferenceTypeMessage(IType type, SourceSpan sourceSpan) : base(sourceSpan)
		{
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public override string Text => $"Cannot dereference a expression of type '{Type.Code}'.";
	}
	public sealed class CannotIndexTypeMessage : ACriticalMessage
	{
		public readonly IType Type;

		public CannotIndexTypeMessage(IType type, SourceSpan sourceSpan) : base(sourceSpan)
		{
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public override string Text => $"Cannot perform a index access on expression of type '{Type.Code}'.";
	}
	public sealed class WrongNumberOfDimensionInIndexMessage : ACriticalMessage
	{
		public readonly int ExpectedIndices;
		public readonly int PassedIndices;

		public WrongNumberOfDimensionInIndexMessage(int expectedIndices, int passedIndices, SourceSpan sourceSpan) : base(sourceSpan)
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

		public FieldNotFoundMessage(IType baseType, CaseInsensitiveString fieldName, SourceSpan sourceSpan) : base(sourceSpan)
		{
			BaseType = baseType ?? throw new ArgumentNullException(nameof(baseType));
			FieldName = fieldName;
		}

		public override string Text => $"The type '{BaseType.Code}' does not have a field '{FieldName}'.";
	}

	public sealed class OnlyVarGlobalInGvlMessage : ACriticalMessage
	{
		public OnlyVarGlobalInGvlMessage(SourceSpan sourceSpan) : base(sourceSpan)
		{
		}

		public override string Text => $"Only VAR_GLOBAL is allowed inside a GVL.";
	}

	public sealed class EnumValueNotFoundMessage : ACriticalMessage
	{
		public readonly EnumTypeSymbol EnumType;
		public readonly CaseInsensitiveString Name;

		public EnumValueNotFoundMessage(EnumTypeSymbol enumType, CaseInsensitiveString name, SourceSpan sourceSpan) : base(sourceSpan)
		{
			EnumType = enumType ?? throw new ArgumentNullException(nameof(enumType));
			Name = name;
		}

		public override string Text => $"The enumtype '{EnumType.Code}' does not contain a value named '{Name}'.";
	}

	public sealed class CannotCallTypeMessage : ACriticalMessage
	{
		public readonly IType CalledType;

		public CannotCallTypeMessage(IType calledType, SourceSpan sourceSpan) : base(sourceSpan)
		{
			CalledType = calledType ?? throw new ArgumentNullException(nameof(calledType));
		}

		public override string Text => $"The type '{CalledType.Code}' is not callable.";
	}
	public sealed class WrongNumberOfArgumentsMessage : ACriticalMessage
	{
		public readonly ICallableTypeSymbol Function;
		public readonly int PassedCount;

		public WrongNumberOfArgumentsMessage(ICallableTypeSymbol function, int passedCount, SourceSpan sourceSpan) : base(sourceSpan)
		{
			Function = function ?? throw new ArgumentNullException(nameof(function));
			PassedCount = passedCount;
		}

		public override string Text => $"The function '{Function.Name}' takes {Function.GetParameterCountWithoutReturn()} arguments, but {PassedCount} arguments were passed.";
	}
	public sealed class NonInputParameterMustBePassedExplicit : ACriticalMessage
	{
		public readonly ParameterVariableSymbol Symbol;

		public NonInputParameterMustBePassedExplicit(ParameterVariableSymbol symbol, SourceSpan sourceSpan) : base(sourceSpan)
		{
			Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
		}

		public override string Text => $"The parameter '{Symbol.Name}' is of type '{Symbol.Kind.Code}' and must be passed explicit.";
	}
	
	public sealed class InoutArgumentMustHaveSameTypeMessage : ACriticalMessage
	{
		public readonly IType Type1;
		public readonly IType Type2;

		public InoutArgumentMustHaveSameTypeMessage(IType type1, IType type2, SourceSpan sourceSpan) : base(sourceSpan)
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

		public ParameterKindDoesNotMatchAssignMessage(ParameterVariableSymbol symbol, IParameterKindToken parameterKind) : base(parameterKind.SourceSpan)
		{
			Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
			ParameterKind = parameterKind ?? throw new ArgumentNullException(nameof(parameterKind));
		}

		public override string Text => $"The argument for a '{Symbol.Kind.Code}' parameter must be passed with the '{Symbol.Kind.AssignCode}'-Assignoperator.";
	}
	
	public sealed class ParameterWasAlreadyPassedMessage : ACriticalMessage
	{
		public readonly ParameterVariableSymbol Symbol;
		public readonly SourceSpan OriginalPosition;

		public ParameterWasAlreadyPassedMessage(ParameterVariableSymbol symbol, SourceSpan originalPosition, SourceSpan duplicatePosition) : base(duplicatePosition)
		{
			Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
			OriginalPosition = originalPosition;
		}

		public override string Text => $"The parameter '{Symbol.Name}' was already passed earlier.";
	}
	public sealed class CannotUsePositionalParameterAfterExplicitMessage : ACriticalMessage
	{
		public CannotUsePositionalParameterAfterExplicitMessage(SourceSpan span) : base(span)
		{
		}

		public override string Text => $"Cannot use a positional parameter after explicit ones.";
	}
	
	public sealed class CannotUseTypeAsLoopIndexMessage : ACriticalMessage
	{
		public readonly IType Type;
		public CannotUseTypeAsLoopIndexMessage(IType type, SourceSpan span) : base(span)
		{
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public override string Text => $"Cannot use the type '{Type.Code}' as index in a for loop. The type must support addition.";
	}
	public sealed class UnknownDurationUnitMessage : ACriticalMessage
	{
		public readonly CaseInsensitiveString UnitText;
		public UnknownDurationUnitMessage(CaseInsensitiveString unitText, SourceSpan span) : base(span)
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
		public VariableCannotHaveInitialValueMessage(SourceSpan span) : base(span)
		{
		}

		public override string Text => "This variable cannot have an initial value.";
	}
	
	public sealed class CannotInferTypeForInitializerMessage : ACriticalMessage
	{
		public CannotInferTypeForInitializerMessage(SourceSpan span) : base(span)
		{
		}

		public override string Text => "Cannot infer a type for this structured initializer.";
	}
	public sealed class CannotUseAnInitializerForThisTypeMessage : ACriticalMessage
	{
		public CannotUseAnInitializerForThisTypeMessage(SourceSpan span) : base(span)
		{
		}

		public override string Text => "Cannot use an initializer for this type.";
	}
	public sealed class TypeDoesNotHaveThisElementMessage : ACriticalMessage
	{
		public TypeDoesNotHaveThisElementMessage(SourceSpan span) : base(span)
		{
		}

		public override string Text => "This type does not have this element.";
	}
	public sealed class DuplicateInitializerElementMessage : ACriticalMessage
	{
		public readonly SourceSpan Original;
		public DuplicateInitializerElementMessage(SourceSpan span, SourceSpan original) : base(span)
		{
			Original = original;
		}

		public override string Text => "Element was already initialzed earlier.";
	}
	public sealed class CannotUsePositionalElementAfterExplicitMessage : ACriticalMessage
	{
		public CannotUsePositionalElementAfterExplicitMessage(SourceSpan span) : base(span)
		{
		}

		public override string Text => "Cannot use an implicit element after an explicit on.";
	}
	public sealed class IndexNotInitializedMessage : ACriticalMessage
	{
		public readonly int Index;
		public IndexNotInitializedMessage(int index, SourceSpan span) : base(span)
		{
			Index = index;
		}

		public override string Text => $"Missing initializer for index '{Index}'.";
	}
	public sealed class FieldNotInitializedMessage : ACriticalMessage
	{
		public readonly FieldVariableSymbol Field;
		public FieldNotInitializedMessage(FieldVariableSymbol field, SourceSpan span) : base(span)
		{
			Field = field ?? throw new ArgumentNullException(nameof(field));
		}

		public override string Text => $"Missing initializer for field '{Field.Name}'.";
	}
	
	public sealed class MissingElementsInInitializerMessage : ACriticalMessage
	{
		public MissingElementsInInitializerMessage(SourceSpan span) : base(span)
		{
		}

		public override string Text => $"Missing initializer.";
	}
	public sealed class CannotUseImplicitInitializerForThisTypeMessage : ACriticalMessage
	{
		public CannotUseImplicitInitializerForThisTypeMessage(SourceSpan span) : base(span)
		{
		}

		public override string Text => $"Cannot use implicit initializer for this type.";
	}
	public sealed class UseOfUnassignedVariableMessage : ACriticalMessage
	{
		public readonly IVariableSymbol Variable;
		public UseOfUnassignedVariableMessage(IVariableSymbol variable, SourceSpan span) : base(span)
		{
			Variable = variable ?? throw new ArgumentNullException(nameof(variable));
		}

		public override string Text => $"Cannot read the variable '{Variable.Name}' before it was assigned.";
	}
	public sealed class VariableMustBeAssignedBeforeEndOfFunctionMessage : ACriticalMessage
	{
		public readonly IVariableSymbol Variable;
		public VariableMustBeAssignedBeforeEndOfFunctionMessage(IVariableSymbol variable, SourceSpan span) : base(span)
		{
			Variable = variable ?? throw new ArgumentNullException(nameof(variable));
		}

		public override string Text => $"The variable '{Variable.Name}' must be assigned a value before the end of the function.";
	}
	public sealed class UnreachableCodeMessage : AUncriticalMessage
	{
		public UnreachableCodeMessage(SourceSpan span) : base(span)
		{
		}

		public override string Text => $"Unreachable code detected.";
	}
	public sealed class ShadowedLocalVariableMessage : ACriticalMessage
	{
		public readonly IVariableSymbol InnerVariable;
		public readonly IVariableSymbol OuterVariable;

		public ShadowedLocalVariableMessage(SourceSpan span, IVariableSymbol innerVariable, IVariableSymbol outerVariable)
			: base(span)
		{
			InnerVariable = innerVariable ?? throw new ArgumentNullException(nameof(innerVariable));
			OuterVariable = outerVariable ?? throw new ArgumentNullException(nameof(outerVariable));
		}

		public override string Text => $"This variable would shadow another variable with the same name.";
	}
	public sealed class CannotInferTypeOfVariableMessage : ACriticalMessage
	{
		public readonly IVariableSymbol Variable;

		public CannotInferTypeOfVariableMessage(SourceSpan span, IVariableSymbol variable)
			: base(span)
		{
			Variable = variable ?? throw new ArgumentNullException(nameof(variable));
		}

		public override string Text => $"Cannot infer the type of the variable '{Variable.Name}', use either an initial value or a type annotation.";
	}
	
	public sealed class CannotUseVariableBeforeItIsDeclaredMessage : ACriticalMessage
	{
		public readonly IVariableSymbol Variable;

		public CannotUseVariableBeforeItIsDeclaredMessage(SourceSpan span, IVariableSymbol variable)
			: base(span)
		{
			Variable = variable ?? throw new ArgumentNullException(nameof(variable));
		}

		public override string Text => $"Cannot use the variable '{Variable.Name}' before is is declared.";
	}
}
