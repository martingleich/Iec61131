using Compiler.Types;
using System;

namespace Compiler
{
	public interface IBoundExpression
	{
		IType Type { get; }

		T Accept<T>(IVisitor<T> visitor);

		public interface IVisitor<T>
		{
			T Visit(LiteralBoundExpression literalBoundExpression);
			T Visit(SizeOfTypeBoundExpression sizeOfTypeBoundExpression);
			T Visit(VariableBoundExpression variableBoundExpression);
			T Visit(ImplicitEnumToBaseTypeCastBoundExpression implicitEnumCastBoundExpression);
			T Accept(AddBoundExpression addBoundExpression);
		}
	}

	public sealed class LiteralBoundExpression : IBoundExpression
	{
		public readonly ILiteralValue Value;

		public LiteralBoundExpression(ILiteralValue value)
		{
			Value = value ?? throw new ArgumentNullException(nameof(value));
		}

		public IType Type => Value.Type;
		T IBoundExpression.Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Visit(this);
	}

	public sealed class SizeOfTypeBoundExpression : IBoundExpression
	{
		public readonly IType Type;
		public SizeOfTypeBoundExpression(IType type)
		{
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		IType IBoundExpression.Type => BuiltInType.DInt;
		T IBoundExpression.Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Visit(this);
	}

	public sealed class VariableBoundExpression : IBoundExpression
	{
		public VariableBoundExpression(ISyntax? originalSyntax, IVariableSymbol variable)
		{
			OriginalSyntax = originalSyntax;
			Variable = variable ?? throw new ArgumentNullException(nameof(variable));
		}

		public ISyntax? OriginalSyntax { get; }
		public IVariableSymbol Variable { get; }
		public IType Type => Variable.Type;

		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Visit(this);
	}
	
	public sealed class ImplicitEnumToBaseTypeCastBoundExpression : IBoundExpression
	{
		public ImplicitEnumToBaseTypeCastBoundExpression(IBoundExpression value)
		{
			Value = value ?? throw new ArgumentNullException(nameof(value));
		}

		public IBoundExpression Value { get; }
		public IType Type => ((EnumTypeSymbol)Value.Type).BaseType;

		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Visit(this);
	}

	public sealed class AddBoundExpression : IBoundExpression
	{
		public IType Type { get; }
		public readonly IBoundExpression Left;
		public readonly IBoundExpression Right;

		public AddBoundExpression(IType type, IBoundExpression left, IBoundExpression right)
		{
			Type = type ?? throw new ArgumentNullException(nameof(type));
			Left = left ?? throw new ArgumentNullException(nameof(left));
			Right = right ?? throw new ArgumentNullException(nameof(right));
		}

		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Accept(this);
	}
}
