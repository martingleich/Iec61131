using Compiler.Messages;
using Compiler.Types;
using System;

namespace Compiler
{
	public interface ISymbol
	{
		public CaseInsensitiveString Name { get; }
		public SourcePosition DeclaringPosition { get; }
	}

	public sealed class FieldSymbol : IVariableSymbol
	{
		public CaseInsensitiveString Name { get; }
		public SourcePosition DeclaringPosition { get; }
		private IType? _type;
		public IType Type => _type ?? throw new InvalidOperationException("Type is not initialized yet");

		public FieldSymbol(SourcePosition declaringPosition, CaseInsensitiveString name)
		{
			DeclaringPosition = declaringPosition;
			Name = name;
		}

		public FieldSymbol(SourcePosition declaringPosition, CaseInsensitiveString name, IType type)
		{
			DeclaringPosition = declaringPosition;
			Name = name;
			_type = type ?? throw new ArgumentNullException(nameof(type));
		}

		internal void _CompleteType(ITypeSymbol type)
		{
			if (_type != null)
				throw new InvalidOperationException("Type is already initialized.");
			_type = type;
		}

		public override string ToString() => $"{Name} : {Type}";
	}
	
	public interface IVariableSymbol : ISymbol
	{
		IType Type { get; }

		public static IVariableSymbol CreateError(SourcePosition declaringPosition, CaseInsensitiveString name) =>
			new ErrorVariableSymbol(declaringPosition, ITypeSymbol.CreateError(declaringPosition, name), name);
	}

	public sealed class ErrorVariableSymbol : IVariableSymbol
	{
		public ErrorVariableSymbol(SourcePosition declaringPosition, IType type, CaseInsensitiveString name)
		{
			DeclaringPosition = declaringPosition;
			Type = type;
			Name = name;
		}

		public IType Type { get; }
		public CaseInsensitiveString Name { get; }
		public SourcePosition DeclaringPosition { get; }
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
		internal ILiteralValue _GetConstantValue(MessageBag messageBag, SourcePosition sourcePosition)
		{
			if (_value != null)
				return _value;

			if (InGetConstantValue)
			{
				messageBag.Add(new RecursiveConstantDeclarationMessage(sourcePosition));
				return _value = new EnumLiteralValue(Type, new UnknownLiteralValue(Type.BaseType));
			}

			InGetConstantValue = true;
			var boundExpression = ExpressionBinder.BindExpression(MaybeScope!, messageBag, MaybeValueSyntax!, Type.BaseType);
			var literalValue = ConstantExpressionEvaluator.EvaluateConstant(boundExpression, messageBag);
			InGetConstantValue = false;
			return _value = new EnumLiteralValue(Type, literalValue!);
		}
	}
}