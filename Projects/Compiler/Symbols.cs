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
	public sealed class EnumVariableSymbol : IVariableSymbol
	{
		public SourcePosition DeclaringPosition { get; }
		public CaseInsensitiveString Name { get; }
		private EnumLiteralValue? _value;
		public EnumLiteralValue Value => _value ?? throw new InvalidOperationException("Value is not initialised yet");
		IType IVariableSymbol.Type => Type;
		public readonly EnumTypeSymbol Type;

		public EnumVariableSymbol(SourcePosition declaringPosition, CaseInsensitiveString name, EnumLiteralValue value)
		{
			DeclaringPosition = declaringPosition;
			Name = name;
			_value = value ?? throw new ArgumentNullException(nameof(value));
			Type = value.Type;
		}

		public override string ToString() => $"{Name} = {Value.InnerValue}";

		private readonly IExpressionSyntax? MaybeValueSyntax;
		private readonly IScope? MaybeScope;
		internal EnumVariableSymbol(IScope scope, SourcePosition declaringPosition, CaseInsensitiveString name, IExpressionSyntax value, EnumTypeSymbol enumTypeSymbol)
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
	public sealed class ParameterVariableSymbol : IVariableSymbol
	{
		public static ParameterVariableSymbol CreateError(int id, SourcePosition position)
			=> CreateError(ImplicitName.ErrorParam(id), position);
		public static ParameterVariableSymbol CreateError(CaseInsensitiveString name, SourcePosition position)
			=> new (ParameterKind.Input, position, name, ITypeSymbol.CreateErrorForVar(position, name));
		public ParameterVariableSymbol(ParameterKind kind, SourcePosition declaringPosition, CaseInsensitiveString name, IType type)
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
	public sealed class FunctionVariableSymbol : IVariableSymbol
	{
		public FunctionVariableSymbol(Types.FunctionTypeSymbol type)
		{
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		IType IVariableSymbol.Type => Type;
		public Types.FunctionTypeSymbol Type { get; }
		public CaseInsensitiveString Name => Type.Name;
		public SourcePosition DeclaringPosition => Type.DeclaringPosition;
		public static FunctionVariableSymbol CreateError(SourcePosition sourcePosition)
			=> new(FunctionTypeSymbol.CreateError(sourcePosition));
		public static FunctionVariableSymbol CreateError(SourcePosition sourcePosition, CaseInsensitiveString name)
			=> new(FunctionTypeSymbol.CreateError(sourcePosition, name));
		public static FunctionVariableSymbol CreateError(SourcePosition sourcePosition, IType returnType)
			=> new(FunctionTypeSymbol.CreateError(sourcePosition, returnType));
		public static FunctionVariableSymbol CreateError(SourcePosition sourcePosition, CaseInsensitiveString name, IType returnType)
			=> new(FunctionTypeSymbol.CreateError(sourcePosition, name, returnType));
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