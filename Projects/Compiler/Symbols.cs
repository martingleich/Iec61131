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

		public interface IVisitor<T>
		{
			T Visit(FunctionVariableSymbol functionVariableSymbol);
			T Visit(ParameterVariableSymbol parameterVariableSymbol);
			T Visit(EnumVariableSymbol enumVariableSymbol);
			T Visit(GlobalVariableSymbol globalVariableSymbol);
			T Visit(ErrorVariableSymbol errorVariableSymbol);
			T Visit(InlineLocalVariableSymbol inlineLocalVariableSymbol);
			T Visit(LocalVariableSymbol localVariableSymbol);
			T Visit(FieldVariableSymbol fieldVariableSymbol);
		}

		T Accept<T>(IVisitor<T> visitor);
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
		public abstract T Accept<T>(IVariableSymbol.IVisitor<T> visitor);
	}
	
	public sealed class FieldVariableSymbol : AVariableSymbol
	{
		private int? _offset;
		public int Offset => _offset is int value ? value : throw new InvalidOperationException();

		public FieldVariableSymbol(SourceSpan declaringSpan, CaseInsensitiveString name, IType type) : base(declaringSpan, name, type)
		{
		}

		internal void _Complete(int offset)
		{
			if (_offset.HasValue)
				throw new InvalidOperationException();
			if (offset < 0)
				throw new ArgumentException($"{nameof(offset)}({offset}) must be non-negative.");
			_offset = offset;
		}
		public override T Accept<T>(IVariableSymbol.IVisitor<T> visitor) => visitor.Visit(this);
	}

	public interface ILocalVariableSymbol : IVariableSymbol
	{
		public int LocalId { get; }
	}
	public sealed class LocalVariableSymbol : AVariableSymbol, ILocalVariableSymbol
	{
		public int LocalId { get; }
		public LocalVariableSymbol(SourceSpan declaringSpan, CaseInsensitiveString name, int localId, IType type, IBoundExpression? initialValue) : base(declaringSpan, name, type)
		{
			LocalId = localId;
			InitialValue = initialValue;
		}
		public readonly IBoundExpression? InitialValue; 
		public override T Accept<T>(IVariableSymbol.IVisitor<T> visitor) => visitor.Visit(this);
	}
	public sealed class InlineLocalVariableSymbol : IVariableSymbol, ILocalVariableSymbol
	{
		public SourceSpan DeclaringSpan { get; }
		public CaseInsensitiveString Name { get; }
		public int LocalId { get; }
		private IType? _type;
		public readonly bool IsReadonly;

		public InlineLocalVariableSymbol(SourceSpan declaringSpan, CaseInsensitiveString name, int localId, bool isReadonly)
		{
			DeclaringSpan = declaringSpan;
			Name = name;
			LocalId = localId;
			IsReadonly = isReadonly;
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
		public T Accept<T>(IVariableSymbol.IVisitor<T> visitor) => visitor.Visit(this);
	}
	
	public sealed class ParameterVariableSymbol : IVariableSymbol
	{
		public static ParameterVariableSymbol CreateError(int id, SourceSpan span)
			=> CreateError(ImplicitName.ErrorParam(id), id, span);
		public static ParameterVariableSymbol CreateError(CaseInsensitiveString name, int id, SourceSpan span)
			=> new (ParameterKind.Input, span, name, id, ITypeSymbol.CreateErrorForVar(span, name));

		public int ParameterId;
		public ParameterVariableSymbol(ParameterKind kind, SourceSpan declaringSpan, CaseInsensitiveString name, int parameterId, IType type)
		{
			Kind = kind ?? throw new ArgumentNullException(nameof(kind));
			DeclaringSpan = declaringSpan;
			Name = name;
			ParameterId = parameterId;
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public ParameterKind Kind { get; }
		public SourceSpan DeclaringSpan { get; }
		public CaseInsensitiveString Name { get; }
		public IType Type { get; }

		public override string ToString() => $"{Kind} {Name} : {Type}";

		public T Accept<T>(IVariableSymbol.IVisitor<T> visitor) => visitor.Visit(this);
	}
	public sealed class ErrorVariableSymbol : AVariableSymbol
	{
		public ErrorVariableSymbol(SourceSpan declaringSpan, CaseInsensitiveString name, IType type) : base(declaringSpan, name, type)
		{
		}
		public override T Accept<T>(IVariableSymbol.IVisitor<T> visitor) => visitor.Visit(this);

	}
	public sealed class GlobalVariableSymbol : AVariableSymbol
	{
		public GlobalVariableSymbol(
			SourceSpan declaringSpan,
			CaseInsensitiveString moduleName,
			CaseInsensitiveString gvlName,
			CaseInsensitiveString name,
			IType type,
			ILiteralValue? initialValue) : base(declaringSpan, name, type)
		{
			ModuleName = moduleName;
			GvlName = gvlName;
			_initialValue = initialValue;
		}
		public GlobalVariableSymbol(
			SourceSpan declaringSpan,
			CaseInsensitiveString moduleName,
			CaseInsensitiveString gvlName,
			CaseInsensitiveString name,
			IType type,
			IExpressionSyntax? initialValueSyntax) : base(declaringSpan, name, type)
		{
			ModuleName = moduleName;
			GvlName = gvlName;
			InitialValueSyntax = initialValueSyntax;
		}
		public readonly CaseInsensitiveString ModuleName;
		public readonly CaseInsensitiveString GvlName;
		public UniqueSymbolId UniqueName => new UniqueSymbolId(new UniqueSymbolId(ModuleName, GvlName).ToCaseInsensitive(), Name);
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
		private readonly IExpressionSyntax? InitialValueSyntax;

		internal void _CompleteInitialValue(IScope scope, MessageBag messages)
		{
			if (InitialValueSyntax != null)
			{
				var boundSyntax = ExpressionBinder.Bind(InitialValueSyntax, scope, messages, Type);
				InitialValue = ConstantExpressionEvaluator.EvaluateConstant(scope.SystemScope, boundSyntax, messages);
			}
		}
		public override T Accept<T>(IVariableSymbol.IVisitor<T> visitor) => visitor.Visit(this);
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
		public T Accept<T>(IVariableSymbol.IVisitor<T> visitor) => visitor.Visit(this);
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
		public UniqueSymbolId UniqueName => Type.UniqueName;
		public SourceSpan DeclaringSpan => Type.DeclaringSpan;
		public static FunctionVariableSymbol CreateError(SourceSpan sourceSpan)
			=> new(FunctionTypeSymbol.CreateError(sourceSpan));
		public static FunctionVariableSymbol CreateError(SourceSpan sourceSpan, CaseInsensitiveString name)
			=> new(FunctionTypeSymbol.CreateError(sourceSpan, name));
		public static FunctionVariableSymbol CreateError(SourceSpan sourceSpan, IType returnType)
			=> new(FunctionTypeSymbol.CreateError(sourceSpan, returnType));
		public static FunctionVariableSymbol CreateError(SourceSpan sourceSpan, CaseInsensitiveString name, IType returnType)
			=> new(FunctionTypeSymbol.CreateError(sourceSpan, name, returnType));

		public override string ToString() => UniqueName.ToString();
		public T Accept<T>(IVariableSymbol.IVisitor<T> visitor) => visitor.Visit(this);
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