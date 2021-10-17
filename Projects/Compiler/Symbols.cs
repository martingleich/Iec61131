using Compiler.Messages;
using Compiler.Scopes;
using Compiler.Types;
using System;

namespace Compiler
{
	public interface ISymbol
	{
		public CaseInsensitiveString Name { get; }
		public SourcePosition DeclaringPosition { get; }
	}

	public interface IVariableSymbol : ISymbol
	{
		IType Type { get; }

		public static IVariableSymbol CreateError(SourcePosition declaringPosition, CaseInsensitiveString name) =>
			new ErrorVariableSymbol(declaringPosition, name, ITypeSymbol.CreateErrorForVar(declaringPosition, name));
	}

	public abstract class AVariableSymbol : IVariableSymbol
	{
		protected AVariableSymbol(SourcePosition declaringPosition, CaseInsensitiveString name, IType type)
		{
			Name = name;
			DeclaringPosition = declaringPosition;
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public CaseInsensitiveString Name { get; }
		public SourcePosition DeclaringPosition { get; }
		public IType Type { get; }
		public override string ToString() => $"{Name} : {Type}";
	}
	public sealed class FieldVariableSymbol : AVariableSymbol
	{
		public FieldVariableSymbol(SourcePosition declaringPosition, CaseInsensitiveString name, IType type) : base(declaringPosition, name, type)
		{
		}
	}

	public sealed class LocalVariableSymbol : AVariableSymbol
	{
		public LocalVariableSymbol(SourcePosition declaringPosition, CaseInsensitiveString name, IType type) : base(declaringPosition, name, type)
		{
		}
	}

	public sealed class ErrorVariableSymbol : AVariableSymbol
	{
		public ErrorVariableSymbol(SourcePosition declaringPosition, CaseInsensitiveString name, IType type) : base(declaringPosition, name, type)
		{
		}

	}

	public sealed class GlobalVariableSymbol : AVariableSymbol
	{
		public GlobalVariableSymbol(SourcePosition declaringPosition, CaseInsensitiveString name, IType type) : base(declaringPosition, name, type)
		{
		}
		public static GlobalVariableSymbol CreateError(SourcePosition declaringPosition, CaseInsensitiveString name) => new (
			declaringPosition, name,
			ITypeSymbol.CreateErrorForVar(declaringPosition, name));
	}

	public sealed class EnumValueSymbol : IVariableSymbol
	{
		public SourcePosition DeclaringPosition { get; }
		public CaseInsensitiveString Name { get; }
		private EnumLiteralValue? _value;
		public EnumLiteralValue Value => _value ?? throw new InvalidOperationException("Value is not initialised yet");
		IType IVariableSymbol.Type => Type;
		public readonly EnumTypeSymbol Type;

		public EnumValueSymbol(SourcePosition declaringPosition, CaseInsensitiveString name, EnumLiteralValue value)
		{
			DeclaringPosition = declaringPosition;
			Name = name;
			_value = value ?? throw new ArgumentNullException(nameof(value));
			Type = value.Type;
		}

		public override string ToString() => $"{Name} = {Value.InnerValue}";

		private readonly IExpressionSyntax? MaybeValueSyntax;
		private readonly IScope? MaybeScope;
		internal EnumValueSymbol(IScope scope, SourcePosition declaringPosition, CaseInsensitiveString name, IExpressionSyntax value, EnumTypeSymbol enumTypeSymbol)
		{
			MaybeScope = scope ?? throw new ArgumentNullException(nameof(scope));
			DeclaringPosition = declaringPosition;
			Name = name;
			MaybeValueSyntax = value ?? throw new ArgumentNullException(nameof(value));
			Type = enumTypeSymbol ?? throw new ArgumentNullException(nameof(enumTypeSymbol));
		}

		private bool InGetConstantValue;
		internal ILiteralValue _GetConstantValue(MessageBag messageBag)
		{
			if (_value != null)
				return _value;

			if (InGetConstantValue)
			{
				messageBag.Add(new RecursiveConstantDeclarationMessage(MaybeValueSyntax!.SourcePosition));
				return _value = new EnumLiteralValue(Type, new UnknownLiteralValue(Type.BaseType));
			}

			InGetConstantValue = true;
			var boundExpression = ExpressionBinder.Bind(MaybeValueSyntax!, MaybeScope!, messageBag, Type.BaseType);
			var literalValue = ConstantExpressionEvaluator.EvaluateConstant(MaybeScope!.SystemScope, boundExpression, messageBag) ?? MaybeScope!.SystemScope.GetDefaultValue(Type.BaseType);
			InGetConstantValue = false;
			return _value = new EnumLiteralValue(Type, literalValue);
		}
	}

	public sealed class FunctionSymbol : ISymbol
	{
		public readonly bool IsError;
		public readonly bool IsProgram;
		public CaseInsensitiveString Name { get; }
		public SourcePosition DeclaringPosition { get; }
		public readonly OrderedSymbolSet<ParameterSymbol> Parameters;

		public FunctionSymbol(bool isProgram, CaseInsensitiveString name, SourcePosition declaringPosition, OrderedSymbolSet<ParameterSymbol> parameters) :
			this(false, isProgram, name, declaringPosition, parameters)
		{
		}
		private FunctionSymbol(bool isError, bool isProgram, CaseInsensitiveString name, SourcePosition declaringPosition, OrderedSymbolSet<ParameterSymbol> parameters)
		{
			IsError = isError;
			IsProgram = isProgram;
			Name = name;
			DeclaringPosition = declaringPosition;
			Parameters = parameters;
		}

		public IType ReturnType => Parameters.TryGetValue(Name, out var returnParam)
			? returnParam.Type
			: NullType.Instance;
		public int ParameterCountWithoutReturn => ReturnType is NullType
			? Parameters.Count
			: Parameters.Count - 1;

		public override string ToString() => $"{Name}";

		public static FunctionSymbol CreateError(SourcePosition sourcePosition)
			=> CreateError(sourcePosition, ImplicitName.ErrorFunction, ITypeSymbol.CreateErrorForFunc(sourcePosition, ImplicitName.ErrorFunction));
		public static FunctionSymbol CreateError(SourcePosition sourcePosition, CaseInsensitiveString name)
			=> CreateError(sourcePosition, name, ITypeSymbol.CreateErrorForFunc(sourcePosition, name));
		public static FunctionSymbol CreateError(SourcePosition sourcePosition, IType returnType)
			=> CreateError(sourcePosition, ImplicitName.ErrorFunction, returnType);
		public static FunctionSymbol CreateError(SourcePosition sourcePosition, CaseInsensitiveString name, IType returnType)
			=> new(true, false, name, sourcePosition, OrderedSymbolSet.ToOrderedSymbolSet(
				new ParameterSymbol(ParameterKind.Output, sourcePosition, name, returnType)));
	}

	public sealed class ParameterSymbol : IVariableSymbol
	{
		public static ParameterSymbol CreateError(int id, SourcePosition position)
			=> CreateError(ImplicitName.ErrorParam(id), position);
		public static ParameterSymbol CreateError(CaseInsensitiveString name, SourcePosition position)
			=> new (ParameterKind.Input, position, name, ITypeSymbol.CreateErrorForVar(position, name));
		public ParameterSymbol(ParameterKind kind, SourcePosition declaringPosition, CaseInsensitiveString name, IType type)
		{
			Kind = kind ?? throw new ArgumentNullException(nameof(kind));
			DeclaringPosition = declaringPosition;
			Name = name;
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public ParameterKind Kind { get; }
		public SourcePosition DeclaringPosition { get; }
		public CaseInsensitiveString Name { get; }
		public IType Type { get; }

		public override string ToString() => $"{Kind} {Name} : {Type}";

	}

	public sealed class ParameterKind : IEquatable<ParameterKind>
	{
		public readonly static ParameterKind Input = new(VarInputToken.DefaultGenerating, AssignToken.DefaultGenerating);
		public readonly static ParameterKind Output = new(VarOutToken.DefaultGenerating, DoubleArrowToken.DefaultGenerating);
		public readonly static ParameterKind InOut = new(VarInOutToken.DefaultGenerating, AssignToken.DefaultGenerating);

		private ParameterKind(string code, string assignCode)
		{
			Code = code ?? throw new ArgumentNullException(nameof(code));
			AssignCode = assignCode ?? throw new ArgumentNullException(nameof(assignCode));
		}

		public string Code { get; }
		public string AssignCode { get; }

		public bool Equals(ParameterKind? other) => other != null && other.Code == Code;
		public override bool Equals(object? obj) => throw new NotImplementedException();
		public override int GetHashCode() => Code.GetHashCode();
		public override string ToString() => Code;

		public static ParameterKind? TryMapDecl(IVarDeclKindToken token) => token.Accept(ParameterKindMapper.Instance);
		private sealed class ParameterKindMapper : IVarDeclKindToken.IVisitor<ParameterKind?>
		{
			public static readonly ParameterKindMapper Instance = new();
			public ParameterKind? Visit(VarToken varToken) => null;
			public ParameterKind? Visit(VarInputToken varInputToken) => Input;
			public ParameterKind? Visit(VarGlobalToken varGlobalToken) => null;
			public ParameterKind? Visit(VarOutToken varOutToken) => Output;
			public ParameterKind? Visit(VarInOutToken varInOutToken) => InOut;
			public ParameterKind? Visit(VarTempToken varTempToken) => null;
		}

		internal bool MatchesAssignKind(IParameterKindToken parameterKind) => parameterKind.Accept(ParameterKindChecker.Instance, this);
		private sealed class ParameterKindChecker : IParameterKindToken.IVisitor<bool, ParameterKind>
		{
			public static readonly ParameterKindChecker Instance = new();

			public bool Visit(AssignToken assignToken, ParameterKind context) => context.Equals(Input) || context.Equals(InOut);
			public bool Visit(DoubleArrowToken doubleArrowToken, ParameterKind context) => context.Equals(Output);
		}
	}

	public sealed class GlobalVariableListSymbol : ISymbol
	{
		public readonly SymbolSet<GlobalVariableSymbol> Variables;

		public GlobalVariableListSymbol(SourcePosition declaringPosition, CaseInsensitiveString name, SymbolSet<GlobalVariableSymbol> variables)
		{
			DeclaringPosition = declaringPosition;
			Name = name;
			Variables = variables;
		}

		public CaseInsensitiveString Name { get; }
		public SourcePosition DeclaringPosition { get; }
	}
}