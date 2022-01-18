using Compiler.Messages;
using Compiler.Scopes;
using Compiler.Types;
using System;

namespace Compiler
{
	public interface ISymbol
	{
		public CaseInsensitiveString Name { get; }
		public SourceSpan DeclaringSpan { get; }
	}

	public interface IVariableSymbol : ISymbol
	{
		IType Type { get; }

		public static IVariableSymbol CreateError(SourceSpan declaringSpan, CaseInsensitiveString name) =>
			new ErrorVariableSymbol(declaringSpan, name, ITypeSymbol.CreateErrorForVar(declaringSpan, name));
	}

	public abstract class AVariableSymbol : IVariableSymbol
	{
		protected AVariableSymbol(SourceSpan declaringSpan, CaseInsensitiveString name, IType type)
		{
			Name = name;
			DeclaringSpan = declaringSpan;
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public CaseInsensitiveString Name { get; }
		public SourceSpan DeclaringSpan { get; }
		public IType Type { get; }
		public override string ToString() => $"{Name} : {Type}";
	}
	
	public sealed class FieldVariableSymbol : AVariableSymbol
	{
		public FieldVariableSymbol(SourceSpan declaringSpan, CaseInsensitiveString name, IType type) : base(declaringSpan, name, type)
		{
		}
	}
	public sealed class LocalVariableSymbol : AVariableSymbol
	{
		public LocalVariableSymbol(SourceSpan declaringSpan, CaseInsensitiveString name, IType type, IBoundExpression? initialValue) : base(declaringSpan, name, type)
		{
			InitialValue = initialValue;
		}
		public readonly IBoundExpression? InitialValue; 
	}
	public sealed class InlineLocalVariableSymbol : IVariableSymbol
	{
		public SourceSpan DeclaringSpan { get; }
		public CaseInsensitiveString Name { get; }
		private IType? _type;

		public InlineLocalVariableSymbol(SourceSpan declaringSpan, CaseInsensitiveString name)
		{
			DeclaringSpan = declaringSpan;
			Name = name;
		}

		public IType Type => _type ?? throw new InvalidOperationException();

		internal bool IsDeclared => _type != null;
		internal bool IsErrorDeclared { get; private set; }
		internal void Declare(IType type)
		{
			if (_type != null && !IsErrorDeclared)
				throw new InvalidOperationException();
			IsErrorDeclared = false;
			_type = type;
		}
		internal void DeclareError(IType type)
		{
			IsErrorDeclared = true;
			_type = type;
		}
	}
	public sealed class ErrorVariableSymbol : AVariableSymbol
	{
		public ErrorVariableSymbol(SourceSpan declaringSpan, CaseInsensitiveString name, IType type) : base(declaringSpan, name, type)
		{
		}

	}
	public sealed class GlobalVariableSymbol : AVariableSymbol
	{
		public GlobalVariableSymbol(
			SourceSpan declaringSpan,
			CaseInsensitiveString moduleName,
			CaseInsensitiveString gvlName,
			CaseInsensitiveString name,
			IType type,
			IBoundExpression? initialValueSyntax) : base(declaringSpan, name, type)
		{
			UniqueName = $"{moduleName}::{gvlName}::{name}";
			InitialValueSyntax = initialValueSyntax;
		}
		public readonly string UniqueName;
		private ILiteralValue? _initialValue;
		public ILiteralValue? InitialValue {
			get
			{
				if(_initialValue == null && InitialValueSyntax != null)
					throw new InvalidOperationException("Initial value was not completed.");
				return _initialValue;
			}
			private set
			{
				if (_initialValue != null)
					throw new InvalidOperationException("Initial value was already completed.");
				_initialValue = value;
			}
		}
		private readonly IBoundExpression? InitialValueSyntax;

		internal void _CompleteInitialValue(SystemScope systemScope, MessageBag messages)
		{
			if (InitialValueSyntax != null)
				InitialValue = ConstantExpressionEvaluator.EvaluateConstant(systemScope, InitialValueSyntax, messages);
		}
	}

	public sealed class EnumVariableSymbol : IVariableSymbol
	{
		public SourceSpan DeclaringSpan { get; }
		public CaseInsensitiveString Name { get; }
		private EnumLiteralValue? _value;
		public EnumLiteralValue Value => _value ?? throw new InvalidOperationException("Value is not initialised yet");
		IType IVariableSymbol.Type => Type;
		public readonly EnumTypeSymbol Type;

		public EnumVariableSymbol(SourceSpan declaringSpan, CaseInsensitiveString name, EnumLiteralValue value)
		{
			DeclaringSpan = declaringSpan;
			Name = name;
			_value = value ?? throw new ArgumentNullException(nameof(value));
			Type = value.Type;
		}

		public override string ToString() => $"{Name} = {Value.InnerValue}";

		private readonly IExpressionSyntax? MaybeValueSyntax;
		private readonly IScope? MaybeScope;
		internal EnumVariableSymbol(IScope scope, SourceSpan declaringSpan, CaseInsensitiveString name, IExpressionSyntax value, EnumTypeSymbol enumTypeSymbol)
		{
			MaybeScope = scope ?? throw new ArgumentNullException(nameof(scope));
			DeclaringSpan = declaringSpan;
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
				messageBag.Add(new RecursiveConstantDeclarationMessage(MaybeValueSyntax!.SourceSpan));
				return _value = new EnumLiteralValue(Type, new UnknownLiteralValue(Type.BaseType));
			}

			InGetConstantValue = true;
			var boundExpression = ExpressionBinder.Bind(MaybeValueSyntax!, MaybeScope!, messageBag, Type.BaseType);
			var literalValue = ConstantExpressionEvaluator.EvaluateConstant(MaybeScope!.SystemScope, boundExpression, messageBag) ?? new UnknownLiteralValue(Type.BaseType);
			InGetConstantValue = false;
			return _value = new EnumLiteralValue(Type, literalValue);
		}
	}
	public sealed class ParameterVariableSymbol : IVariableSymbol
	{
		public static ParameterVariableSymbol CreateError(int id, SourceSpan span)
			=> CreateError(ImplicitName.ErrorParam(id), span);
		public static ParameterVariableSymbol CreateError(CaseInsensitiveString name, SourceSpan span)
			=> new (ParameterKind.Input, span, name, ITypeSymbol.CreateErrorForVar(span, name));
		public ParameterVariableSymbol(ParameterKind kind, SourceSpan declaringSpan, CaseInsensitiveString name, IType type)
		{
			Kind = kind ?? throw new ArgumentNullException(nameof(kind));
			DeclaringSpan = declaringSpan;
			Name = name;
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public ParameterKind Kind { get; }
		public SourceSpan DeclaringSpan { get; }
		public CaseInsensitiveString Name { get; }
		public IType Type { get; }

		public override string ToString() => $"{Kind} {Name} : {Type}";

	}
	public sealed class FunctionVariableSymbol : IVariableSymbol
	{
		public FunctionVariableSymbol(FunctionTypeSymbol type)
		{
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		IType IVariableSymbol.Type => Type;
		public FunctionTypeSymbol Type { get; }
		public CaseInsensitiveString Name => Type.Name;
		public UniqueSymbolId UniqueId => Type.UniqueId;
		public SourceSpan DeclaringSpan => Type.DeclaringSpan;
		public static FunctionVariableSymbol CreateError(SourceSpan sourceSpan)
			=> new(FunctionTypeSymbol.CreateError(sourceSpan));
		public static FunctionVariableSymbol CreateError(SourceSpan sourceSpan, CaseInsensitiveString name)
			=> new(FunctionTypeSymbol.CreateError(sourceSpan, name));
		public static FunctionVariableSymbol CreateError(SourceSpan sourceSpan, IType returnType)
			=> new(FunctionTypeSymbol.CreateError(sourceSpan, returnType));
		public static FunctionVariableSymbol CreateError(SourceSpan sourceSpan, CaseInsensitiveString name, IType returnType)
			=> new(FunctionTypeSymbol.CreateError(sourceSpan, name, returnType));

		public override string ToString() => UniqueId.ToString();
	}

	public sealed class GlobalVariableListSymbol : IScopeSymbol
	{
		public readonly SymbolSet<GlobalVariableSymbol> Variables;

		public GlobalVariableListSymbol(SourceSpan declaringSpan, CaseInsensitiveString name, SymbolSet<GlobalVariableSymbol> variables)
		{
			DeclaringSpan = declaringSpan;
			Name = name;
			Variables = variables;
		}

		public CaseInsensitiveString Name { get; }
		public SourceSpan DeclaringSpan { get; }

		public ErrorsAnd<IScopeSymbol> LookupScope(CaseInsensitiveString identifier, SourceSpan errorPosition)
			=> EmptyScopeHelper.LookupScope(Name, identifier, errorPosition);
		public ErrorsAnd<ITypeSymbol> LookupType(CaseInsensitiveString identifier, SourceSpan errorPosition)
			=> EmptyScopeHelper.LookupType(Name, identifier, errorPosition);
		public ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourceSpan errorPosition) => Variables.TryGetValue(identifier, out var symbol)
			? ErrorsAnd.Create<IVariableSymbol>(symbol)
			: EmptyScopeHelper.LookupVariable(Name, identifier, errorPosition);
	}

	public interface IScopeSymbol : ISymbol
	{
		ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourceSpan errorPosition);
		ErrorsAnd<ITypeSymbol> LookupType(CaseInsensitiveString identifier, SourceSpan errorPosition);
		ErrorsAnd<IScopeSymbol> LookupScope(CaseInsensitiveString identifier, SourceSpan errorPosition);

		public static IScopeSymbol CreateError(CaseInsensitiveString identifier, SourceSpan errorPosition)
			=> new ErrorScopeSymbol(identifier, errorPosition);
	}

	public static class EmptyScopeHelper
	{
		public static ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString scopeName, CaseInsensitiveString identifier, SourceSpan errorPosition)
			=> ErrorsAnd.Create(IVariableSymbol.CreateError(errorPosition, identifier), new VariableNotFoundMessage(identifier, errorPosition));
		public static ErrorsAnd<ITypeSymbol> LookupType(CaseInsensitiveString scopeName,CaseInsensitiveString identifier, SourceSpan errorPosition)
			=> ErrorsAnd.Create(ITypeSymbol.CreateError(errorPosition, identifier), new TypeNotFoundMessage(identifier, errorPosition));
		public static ErrorsAnd<IScopeSymbol> LookupScope(CaseInsensitiveString scopeName, CaseInsensitiveString identifier, SourceSpan errorPosition)
			=> ErrorsAnd.Create(IScopeSymbol.CreateError(identifier, errorPosition), new ScopeNotFoundMessage(identifier, errorPosition));
	}

	public sealed class ErrorScopeSymbol : IScopeSymbol
	{
		public ErrorScopeSymbol(CaseInsensitiveString name, SourceSpan declaringSpan)
		{
			Name = name;
			DeclaringSpan = declaringSpan;
		}

		public CaseInsensitiveString Name { get; }
		public SourceSpan DeclaringSpan { get; }
		public ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourceSpan errorPosition) => EmptyScopeHelper.LookupVariable(Name, identifier, errorPosition);
		public ErrorsAnd<ITypeSymbol> LookupType(CaseInsensitiveString identifier, SourceSpan errorPosition) => EmptyScopeHelper.LookupType(Name, identifier, errorPosition);
		public ErrorsAnd<IScopeSymbol> LookupScope(CaseInsensitiveString identifier, SourceSpan errorPosition) => EmptyScopeHelper.LookupScope(Name, identifier, errorPosition);
	}
}