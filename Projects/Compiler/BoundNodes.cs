using Compiler.Types;
using System;
using System.Collections.Immutable;

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
			T Accept(BinaryOperatorBoundExpression binaryOperatorBoundExpression);
			T Visit(ImplicitPointerTypeCastBoundExpression implicitPointerTypeCaseBoundExpression);
			T Accept(ImplicitArithmeticCastBoundExpression implicitArithmeticCaseBoundExpression);
		}
	}

	public interface IBoundStatement
	{
		T Accept<T>(IVisitor<T> visitor);
		interface IVisitor<T>
		{
			T Accept(SequenceBoundStatement sequenceBoundStatement);
			T Accept(ExpressionBoundStatement expressionBoundStatement);
			T Accept(AssignBoundStatement assignToExpressionBoundStatement);
			T Accept(IfBoundStatement ifBoundStatement);
			T Accept(WhileBoundStatement whileBoundStatement);
			T Accept(ExitBoundStatement exitBoundStatement);
			T Accept(ContinueBoundStatement continueBoundStatement);
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
		public readonly IType ArgType;
		public SizeOfTypeBoundExpression(IType argType, IType type)
		{
			ArgType = argType ?? throw new ArgumentNullException(nameof(argType));
			Type = type;
		}

		public IType Type { get; }
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
	public sealed class ImplicitPointerTypeCastBoundExpression : IBoundExpression
	{
		public ImplicitPointerTypeCastBoundExpression(IBoundExpression value, PointerType targetType)
		{
			Value = value ?? throw new ArgumentNullException(nameof(value));
			Type = targetType ?? throw new ArgumentNullException(nameof(targetType));
		}

		public readonly IBoundExpression Value;
		public readonly PointerType Type;
		IType IBoundExpression.Type => Type;

		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Visit(this);
	}
	public sealed class ImplicitArithmeticCastBoundExpression : IBoundExpression
	{
		public readonly IBoundExpression Value;

		public ImplicitArithmeticCastBoundExpression(IBoundExpression value, IType type)
		{
			Value = value ?? throw new ArgumentNullException(nameof(value));
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public IType Type { get; }
		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Accept(this);
	}

	public sealed class BinaryOperatorBoundExpression : IBoundExpression
	{
		public IType Type { get; }
		public readonly IBoundExpression Left;
		public readonly IBoundExpression Right;
		public readonly FunctionSymbol Function;

		public BinaryOperatorBoundExpression(IType type, IBoundExpression left, IBoundExpression right, FunctionSymbol function)
		{
			Type = type ?? throw new ArgumentNullException(nameof(type));
			Left = left ?? throw new ArgumentNullException(nameof(left));
			Right = right ?? throw new ArgumentNullException(nameof(right));
			Function = function ?? throw new ArgumentNullException(nameof(function));
		}

		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Accept(this);
	}

	public sealed class SequenceBoundStatement : IBoundStatement
	{
		public readonly ImmutableArray<IBoundStatement> Statements;

		public SequenceBoundStatement(ImmutableArray<IBoundStatement> statements)
		{
			Statements = statements;
		}

		public T Accept<T>(IBoundStatement.IVisitor<T> visitor) => visitor.Accept(this);
	}
	
	public sealed class ExpressionBoundStatement : IBoundStatement
	{
		public readonly IBoundExpression Expression;

		public ExpressionBoundStatement(IBoundExpression expression)
		{
			Expression = expression ?? throw new ArgumentNullException(nameof(expression));
		}

		public T Accept<T>(IBoundStatement.IVisitor<T> visitor) => visitor.Accept(this);
	}

	public sealed class AssignBoundStatement : IBoundStatement
	{
		public readonly IBoundExpression LeftSide;
		public readonly IBoundExpression RightSide;

		public AssignBoundStatement(IBoundExpression leftSide, IBoundExpression rightSide)
		{
			LeftSide = leftSide ?? throw new ArgumentNullException(nameof(leftSide));
			RightSide = rightSide ?? throw new ArgumentNullException(nameof(rightSide));
		}

		public T Accept<T>(IBoundStatement.IVisitor<T> visitor) => visitor.Accept(this);
	}

	public sealed class IfBoundStatement : IBoundStatement
	{
		public sealed class Branch
		{
			public readonly IBoundExpression? Condition;
			public readonly IBoundStatement Body;

			public Branch(IBoundExpression? condition, IBoundStatement body)
			{
				Condition = condition;
				Body = body ?? throw new ArgumentNullException(nameof(body));
			}
		}
		public readonly ImmutableArray<Branch> Branches;

		public IfBoundStatement(ImmutableArray<Branch> branches)
		{
			Branches = branches;
		}

		T IBoundStatement.Accept<T>(IBoundStatement.IVisitor<T> visitor) => visitor.Accept(this);
	}

	public sealed class WhileBoundStatement : IBoundStatement
	{
		public readonly IBoundExpression Condition;
		public readonly IBoundStatement Body;

		public WhileBoundStatement(IBoundExpression condition, IBoundStatement body)
		{
			Condition = condition ?? throw new ArgumentNullException(nameof(condition));
			Body = body ?? throw new ArgumentNullException(nameof(body));
		}

		T IBoundStatement.Accept<T>(IBoundStatement.IVisitor<T> visitor) => visitor.Accept(this);
	}

	public sealed class ExitBoundStatement : IBoundStatement
	{
		T IBoundStatement.Accept<T>(IBoundStatement.IVisitor<T> visitor) => visitor.Accept(this);
	}
	public sealed class ContinueBoundStatement : IBoundStatement
	{
		T IBoundStatement.Accept<T>(IBoundStatement.IVisitor<T> visitor) => visitor.Accept(this);
	}
}
