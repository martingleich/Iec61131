using Compiler.Types;
using System;
using System.Collections.Immutable;

namespace Compiler
{
	public interface IBoundExpression
	{
		IType Type { get; }
		INode OriginalNode { get; }

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
			T Accept(UnaryOperatorBoundExpression unaryOperatorBoundExpression);
			T Visit(PointerDiffrenceBoundExpression pointerDiffrenceBoundExpression);
			T Accept(PointerOffsetBoundExpression pointerOffsetBoundExpression);
			T Accept(DerefBoundExpression derefBoundExpression);
			T Accept(ImplicitAliasToBaseTypeCastBoundExpression aliasToBaseTypeCastBoundExpression);
			T Accept(ImplicitErrorCastBoundExpression implicitErrorCastBoundExpression);
			T Accept(ImplicitAliasFromBaseTypeCastBoundExpression implicitAliasFromBaseTypeCastBoundExpression);
			T Accept(ArrayIndexAccessBoundExpression arrayIndexAccessBoundExpression);
			T Accept(PointerIndexAccessBoundExpression pointerIndexAccessBoundExpression);
			T Accept(FieldAccessBoundExpression fieldAccessBoundExpression);
			T Accept(StaticVariableBoundExpression staticVariableBoundExpression);
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
		public INode OriginalNode { get; }

		public LiteralBoundExpression(INode originalNode, ILiteralValue value)
		{
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
			Value = value ?? throw new ArgumentNullException(nameof(value));
		}

		public IType Type => Value.Type;
		T IBoundExpression.Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Visit(this);
	}

	public sealed class SizeOfTypeBoundExpression : IBoundExpression
	{
		public readonly IType ArgType;
		public INode OriginalNode { get; }
		public SizeOfTypeBoundExpression(INode originalNode, IType argType, IType type)
		{
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
			ArgType = argType ?? throw new ArgumentNullException(nameof(argType));
			Type = type;
		}

		public IType Type { get; }
		T IBoundExpression.Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Visit(this);
	}

	public sealed class VariableBoundExpression : IBoundExpression
	{
		public INode OriginalNode { get; }
		public VariableBoundExpression(ISyntax originalNode, IVariableSymbol variable)
		{
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
			Variable = variable ?? throw new ArgumentNullException(nameof(variable));
		}

		public IVariableSymbol Variable { get; }
		public IType Type => Variable.Type;

		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Visit(this);
	}
	
	public sealed class ImplicitEnumToBaseTypeCastBoundExpression : IBoundExpression
	{
		public INode OriginalNode { get; }
		public ImplicitEnumToBaseTypeCastBoundExpression(INode originalNode, IBoundExpression value)
		{
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
			Value = value ?? throw new ArgumentNullException(nameof(value));
		}

		public IBoundExpression Value { get; }
		public IType Type => ((EnumTypeSymbol)Value.Type).BaseType;

		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Visit(this);
	}
	public sealed class ImplicitPointerTypeCastBoundExpression : IBoundExpression
	{
		public INode OriginalNode { get; }
		public ImplicitPointerTypeCastBoundExpression(INode originalNode, IBoundExpression value, PointerType targetType)
		{
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
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
		public INode OriginalNode { get; }

		public ImplicitArithmeticCastBoundExpression(INode originalNode, IBoundExpression value, IType type)
		{
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
			Value = value ?? throw new ArgumentNullException(nameof(value));
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public IType Type { get; }
		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Accept(this);
	}
	public sealed class ImplicitErrorCastBoundExpression : IBoundExpression
	{
		public readonly IBoundExpression Value;
		public INode OriginalNode { get; }

		public ImplicitErrorCastBoundExpression(INode originalNode, IBoundExpression value, IType type)
		{
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
			Value = value ?? throw new ArgumentNullException(nameof(value));
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public IType Type { get; }
		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Accept(this);
	}
	public sealed class ImplicitAliasToBaseTypeCastBoundExpression : IBoundExpression
	{
		public readonly IBoundExpression Value;
		public INode OriginalNode { get; }

		public ImplicitAliasToBaseTypeCastBoundExpression(INode originalNode, IBoundExpression value, IType type)
		{
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
			Value = value ?? throw new ArgumentNullException(nameof(value));
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public IType Type { get; }

		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Accept(this);
	}
	public sealed class ImplicitAliasFromBaseTypeCastBoundExpression : IBoundExpression
	{
		public readonly IBoundExpression Value;
		public INode OriginalNode { get; }

		public ImplicitAliasFromBaseTypeCastBoundExpression(INode originalNode, IBoundExpression value, IType type)
		{
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
			Value = value ?? throw new ArgumentNullException(nameof(value));
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public IType Type { get; }

		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Accept(this);
	}
	
	public sealed class PointerDiffrenceBoundExpression : IBoundExpression
	{
		public INode OriginalNode { get; }
		public readonly IBoundExpression Left;
		public readonly IBoundExpression Right;

		public PointerDiffrenceBoundExpression(INode originalNode, IBoundExpression left, IBoundExpression right, IType type)
		{
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
			Left = left ?? throw new ArgumentNullException(nameof(left));
			Right = right ?? throw new ArgumentNullException(nameof(right));
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public IType Type { get; }

		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Visit(this);
	}
	public sealed class PointerOffsetBoundExpression : IBoundExpression
	{
		public INode OriginalNode { get; }
		public readonly IBoundExpression Left;
		public readonly IBoundExpression Right;

		public PointerOffsetBoundExpression(INode originalNode, IBoundExpression left, IBoundExpression right, IType type)
		{
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
			Left = left ?? throw new ArgumentNullException(nameof(left));
			Right = right ?? throw new ArgumentNullException(nameof(right));
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public IType Type { get; }

		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Accept(this);
	}

	public sealed class BinaryOperatorBoundExpression : IBoundExpression
	{
		public INode OriginalNode { get; }
		public IType Type { get; }
		public readonly IBoundExpression Left;
		public readonly IBoundExpression Right;
		public readonly FunctionSymbol Function;

		public BinaryOperatorBoundExpression(INode originalNode, IType type, IBoundExpression left, IBoundExpression right, FunctionSymbol function)
		{
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
			Type = type ?? throw new ArgumentNullException(nameof(type));
			Left = left ?? throw new ArgumentNullException(nameof(left));
			Right = right ?? throw new ArgumentNullException(nameof(right));
			Function = function ?? throw new ArgumentNullException(nameof(function));
		}

		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Accept(this);
	}
	public sealed class UnaryOperatorBoundExpression : IBoundExpression
	{
		public INode OriginalNode { get; }
		public IType Type { get; }
		public readonly IBoundExpression Value;
		public readonly FunctionSymbol Function;

		public UnaryOperatorBoundExpression(INode originalNode, IType type, IBoundExpression value, FunctionSymbol function)
		{
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
			Type = type ?? throw new ArgumentNullException(nameof(type));
			Value = value ?? throw new ArgumentNullException(nameof(value));
			Function = function ?? throw new ArgumentNullException(nameof(function));
		}

		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Accept(this);
	}

	public sealed class DerefBoundExpression : IBoundExpression
	{
		public INode OriginalNode { get; }
		public IType Type { get; }
		public readonly IBoundExpression Value;

		public DerefBoundExpression(INode originalNode, IBoundExpression value, IType type)
		{
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
			Type = type ?? throw new ArgumentNullException(nameof(type));
			Value = value ?? throw new ArgumentNullException(nameof(value));
		}

		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Accept(this);
	}
	public sealed class ArrayIndexAccessBoundExpression : IBoundExpression
	{
		public INode OriginalNode { get; }
		public IType Type { get; }
		public readonly ImmutableArray<IBoundExpression> Indices;

		public ArrayIndexAccessBoundExpression(INode originalNode, IType type, ImmutableArray<IBoundExpression> indices)
		{
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
			Type = type ?? throw new ArgumentNullException(nameof(type));
			Indices = indices;
		}

		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Accept(this);
	}
	public sealed class PointerIndexAccessBoundExpression : IBoundExpression
	{
		public INode OriginalNode { get; }
		public IType Type { get; }
		public readonly ImmutableArray<IBoundExpression> Indices;

		public PointerIndexAccessBoundExpression(INode originalNode, IType type, ImmutableArray<IBoundExpression> indices)
		{
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
			Type = type ?? throw new ArgumentNullException(nameof(type));
			Indices = indices;
		}

		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Accept(this);
	}
	public sealed class FieldAccessBoundExpression : IBoundExpression
	{
		public INode OriginalNode { get; }
		public IType Type => Field.Type;
		public readonly IBoundExpression BaseExpression;
		public readonly FieldVariableSymbol Field;

		public FieldAccessBoundExpression(INode originalNode, IBoundExpression baseExpression, FieldVariableSymbol field)
		{
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
			BaseExpression = baseExpression ?? throw new ArgumentNullException(nameof(baseExpression));
			Field = field ?? throw new ArgumentNullException(nameof(field));
		}

		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Accept(this);
	}

	public sealed class StaticVariableBoundExpression : IBoundExpression
	{
		public INode OriginalNode { get; }
		public IType Type => Variable.Type;
		public readonly GlobalVariableSymbol Variable;

		public StaticVariableBoundExpression(INode originalNode, GlobalVariableSymbol variable)
		{
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
			Variable = variable ?? throw new ArgumentNullException(nameof(variable));
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
