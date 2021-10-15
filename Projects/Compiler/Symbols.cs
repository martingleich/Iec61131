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
		public bool IsProgram { get; }
		public CaseInsensitiveString Name { get; }
		public SourcePosition DeclaringPosition { get; }
		public readonly OrderedSymbolSet<ParameterSymbol> Parameters;

		public FunctionSymbol(bool isProgram, CaseInsensitiveString name, SourcePosition declaringPosition, OrderedSymbolSet<ParameterSymbol> parameters)
		{
			IsProgram = isProgram;
			Name = name;
			DeclaringPosition = declaringPosition;
			Parameters = parameters;
		}

		public override string ToString() => $"{Name}";

		public IType? ReturnType => Parameters.TryGetValue(Name)?.Type;

		public static FunctionSymbol CreateError(SourcePosition sourcePosition)
			=> new (false, "__ERROR__".ToCaseInsensitive(), sourcePosition, OrderedSymbolSet<ParameterSymbol>.Empty);
		public static FunctionSymbol CreateError(SourcePosition sourcePosition, IType returnType)
			=> new(false, "__ERROR__".ToCaseInsensitive(), sourcePosition, OrderedSymbolSet<ParameterSymbol>.Create(
				new[] { new ParameterSymbol(ParameterKind.Output, sourcePosition, "__ERROR__".ToCaseInsensitive(), returnType) }));
	}

	public sealed class ParameterSymbol : IVariableSymbol
	{
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
		public readonly static ParameterKind Input = new(VarInputToken.DefaultGenerating);
		public readonly static ParameterKind Output = new(VarOutToken.DefaultGenerating);
		public readonly static ParameterKind InOut = new(VarInOutToken.DefaultGenerating);

		private ParameterKind(string code)
		{
			Code = code ?? throw new ArgumentNullException(nameof(code));
		}

		public string Code { get; }

		public bool Equals(ParameterKind? other) => other != null && other.Code == Code;
		public override bool Equals(object? obj) => throw new NotImplementedException();
		public override int GetHashCode() => Code.GetHashCode();
		public override string ToString() => Code;

		public static ParameterKind? TryMap(IVarDeclKindToken token) => token.Accept(ParameterKindMapper.Instance);
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