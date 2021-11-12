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

	public interface ICallableSymbol : ISymbol
	{
		public OrderedSymbolSet<ParameterVariableSymbol> Parameters { get; }
	}
	public static class CallableSymbol
	{
		public static IType GetReturnType(this ICallableSymbol self) => self.Parameters.TryGetValue(self.Name, out var returnParam)
			? returnParam.Type
			: NullType.Instance;
		public static int GetParameterCountWithoutReturn(this ICallableSymbol self) => self.GetReturnType() is NullType
			? self.Parameters.Count
			: self.Parameters.Count - 1;
	}

	public sealed class FunctionSymbol : ICallableSymbol
	{
		public readonly bool IsError;
		public readonly bool IsProgram;
		public CaseInsensitiveString Name { get; }
		public SourcePosition DeclaringPosition { get; }
		public OrderedSymbolSet<ParameterVariableSymbol> Parameters { get; }

		public FunctionSymbol(bool isProgram, CaseInsensitiveString name, SourcePosition declaringPosition, OrderedSymbolSet<ParameterVariableSymbol> parameters) :
			this(false, isProgram, name, declaringPosition, parameters)
		{
		}
		private FunctionSymbol(bool isError, bool isProgram, CaseInsensitiveString name, SourcePosition declaringPosition, OrderedSymbolSet<ParameterVariableSymbol> parameters)
		{
			IsError = isError;
			IsProgram = isProgram;
			Name = name;
			DeclaringPosition = declaringPosition;
			Parameters = parameters;
		}

		public override string ToString() => $"{Name}";

		public static FunctionSymbol CreateError(SourcePosition sourcePosition)
			=> CreateError(sourcePosition, ImplicitName.ErrorFunction, ITypeSymbol.CreateErrorForFunc(sourcePosition, ImplicitName.ErrorFunction));
		public static FunctionSymbol CreateError(SourcePosition sourcePosition, CaseInsensitiveString name)
			=> CreateError(sourcePosition, name, ITypeSymbol.CreateErrorForFunc(sourcePosition, name));
		public static FunctionSymbol CreateError(SourcePosition sourcePosition, IType returnType)
			=> CreateError(sourcePosition, ImplicitName.ErrorFunction, returnType);
		public static FunctionSymbol CreateError(SourcePosition sourcePosition, CaseInsensitiveString name, IType returnType)
			=> new(true, false, name, sourcePosition, OrderedSymbolSet.ToOrderedSymbolSet(
				new ParameterVariableSymbol(ParameterKind.Output, sourcePosition, name, returnType)));
	}
	public sealed class FunctionBlockSymbol : ICallableSymbol, ITypeSymbol, _IDelayedLayoutType
	{
		public string Code => Name.Original;
		public SourcePosition DeclaringPosition { get; }
		public CaseInsensitiveString Name { get; }

		private readonly StructuredLayoutHelper _layoutHelper;
		public SymbolSet<FieldVariableSymbol> Fields => !_fields.IsDefault ? _fields : throw new InvalidOperationException("Fields is not initialized.");
		private SymbolSet<FieldVariableSymbol> _fields;

		public OrderedSymbolSet<ParameterVariableSymbol> Parameters => !_parameters.IsDefault ? _parameters : throw new InvalidOperationException("Parameters is not initialized.");
		private OrderedSymbolSet<ParameterVariableSymbol> _parameters;

		public FunctionBlockSymbol(SourcePosition declaringPosition, CaseInsensitiveString name)
		{
			DeclaringPosition = declaringPosition;
			Name = name;
			_layoutHelper = new StructuredLayoutHelper();
		}

		public T Accept<T, TContext>(IType.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
		public LayoutInfo LayoutInfo => throw new NotImplementedException();

		internal void _SetFields(SymbolSet<FieldVariableSymbol> fields)
		{
			if (!_fields.IsDefault)
				throw new InvalidOperationException();
			_fields = fields;
		}
		internal void _SetParameters(OrderedSymbolSet<ParameterVariableSymbol> parameters)
		{
			if (!_parameters.IsDefault)
				throw new InvalidOperationException();
			_parameters = parameters;
		}

		UndefinedLayoutInfo _IDelayedLayoutType.GetLayoutInfo(MessageBag messageBag, SourcePosition position) => _layoutHelper.GetLayoutInfo(
			messageBag,
			position,
			false,
			Fields);

		void _IDelayedLayoutType.RecursiveLayout(MessageBag messageBag, SourcePosition position) => _layoutHelper.GetLayoutInfo(
			messageBag,
			position,
			false,
			Fields);
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