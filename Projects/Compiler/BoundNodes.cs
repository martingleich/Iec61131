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
			T Visit(ImplicitArithmeticCastBoundExpression implicitArithmeticCaseBoundExpression);
			T Visit(UnaryOperatorBoundExpression unaryOperatorBoundExpression);
			T Visit(PointerDiffrenceBoundExpression pointerDiffrenceBoundExpression);
			T Visit(PointerOffsetBoundExpression pointerOffsetBoundExpression);
			T Visit(DerefBoundExpression derefBoundExpression);
			T Visit(ImplicitAliasToBaseTypeCastBoundExpression aliasToBaseTypeCastBoundExpression);
			T Visit(ImplicitErrorCastBoundExpression implicitErrorCastBoundExpression);
			T Visit(ImplicitAliasFromBaseTypeCastBoundExpression implicitAliasFromBaseTypeCastBoundExpression);
			T Visit(ArrayIndexAccessBoundExpression arrayIndexAccessBoundExpression);
			T Visit(PointerIndexAccessBoundExpression pointerIndexAccessBoundExpression);
			T Visit(FieldAccessBoundExpression fieldAccessBoundExpression);
			T Visit(StaticVariableBoundExpression staticVariableBoundExpression);
			T Visit(FunctionCallBoundExpression functionCallBoundExpression);
			T Visit(ImplicitDiscardBoundExpression implicitDiscardBoundExpression);
			T Visit(FunctionBlockCallBoundExpression functionBlockCallBoundExpression);
		}
	}

	public interface IBoundStatement
	{
		T Accept<T>(IVisitor<T> visitor);
		interface IVisitor<T>
		{
			T Visit(SequenceBoundStatement sequenceBoundStatement);
			T Visit(ExpressionBoundStatement expressionBoundStatement);
			T Visit(AssignBoundStatement assignToExpressionBoundStatement);
			T Visit(IfBoundStatement ifBoundStatement);
			T Visit(WhileBoundStatement whileBoundStatement);
			T Visit(ExitBoundStatement exitBoundStatement);
			T Visit(ContinueBoundStatement continueBoundStatement);
			T Visit(ReturnBoundStatement returnBoundStatement);
			T Visit(ForLoopBoundStatement forLoopBoundStatement);
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
		public INode OriginalNode => Value.OriginalNode;
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
		public INode OriginalNode => Value.OriginalNode;
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
		public INode OriginalNode => Value.OriginalNode;

		public ImplicitArithmeticCastBoundExpression(IBoundExpression value, IType type)
		{
			Value = value ?? throw new ArgumentNullException(nameof(value));
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public IType Type { get; }
		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Visit(this);
	}
	public sealed class ImplicitErrorCastBoundExpression : IBoundExpression
	{
		public readonly IBoundExpression Value;
		public INode OriginalNode => Value.OriginalNode;

		public ImplicitErrorCastBoundExpression(IBoundExpression value, IType type)
		{
			Value = value ?? throw new ArgumentNullException(nameof(value));
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public IType Type { get; }
		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Visit(this);
	}
	public sealed class ImplicitAliasToBaseTypeCastBoundExpression : IBoundExpression
	{
		public readonly IBoundExpression Value;
		public INode OriginalNode => Value.OriginalNode;

		public ImplicitAliasToBaseTypeCastBoundExpression(IBoundExpression value, IType type)
		{
			Value = value ?? throw new ArgumentNullException(nameof(value));
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public IType Type { get; }

		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Visit(this);
	}
	public sealed class ImplicitAliasFromBaseTypeCastBoundExpression : IBoundExpression
	{
		public readonly IBoundExpression Value;
		public INode OriginalNode => Value.OriginalNode;

		public ImplicitAliasFromBaseTypeCastBoundExpression(IBoundExpression value, IType type)
		{
			Value = value ?? throw new ArgumentNullException(nameof(value));
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public IType Type { get; }

		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Visit(this);
	}
	public sealed class ImplicitDiscardBoundExpression : IBoundExpression
	{
		public IType Type => NullType.Instance;
		public INode OriginalNode => Value.OriginalNode;
		public readonly IBoundExpression Value;

		public ImplicitDiscardBoundExpression(IBoundExpression value)
		{
			Value = value ?? throw new ArgumentNullException(nameof(value));
		}

		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Visit(this);
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

		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Visit(this);
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

		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Visit(this);
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

		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Visit(this);
	}
	public sealed class ArrayIndexAccessBoundExpression : IBoundExpression
	{
		public INode OriginalNode { get; }
		public readonly IBoundExpression Base;
		public IType Type { get; }
		public readonly ImmutableArray<IBoundExpression> Indices;

		public ArrayIndexAccessBoundExpression(INode originalNode, IBoundExpression @base, IType type, ImmutableArray<IBoundExpression> indices)
		{
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
			Base = @base ?? throw new ArgumentNullException(nameof(@base));
			Type = type ?? throw new ArgumentNullException(nameof(type));
			Indices = indices;
		}

		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Visit(this);
	}
	public sealed class PointerIndexAccessBoundExpression : IBoundExpression
	{
		public INode OriginalNode { get; }
		public readonly IBoundExpression Base;
		public IType Type { get; }
		public readonly ImmutableArray<IBoundExpression> Indices;

		public PointerIndexAccessBoundExpression(INode originalNode, IBoundExpression @base, IType type, ImmutableArray<IBoundExpression> indices)
		{
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
			Base = @base ?? throw new ArgumentNullException(nameof(@base));
			Type = type ?? throw new ArgumentNullException(nameof(type));
			Indices = indices;
		}

		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Visit(this);
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

		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Visit(this);
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

		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Visit(this);
	}

	public readonly struct BoundCallArgument
	{
		public readonly ParameterVariableSymbol ParameterSymbol;
		public readonly IBoundExpression Parameter;
		public readonly IBoundExpression Value;

		public BoundCallArgument(ParameterVariableSymbol parameterSymbol, IBoundExpression parameter, IBoundExpression value)
		{
			Parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
			Value = value ?? throw new ArgumentNullException(nameof(value));
			ParameterSymbol = parameterSymbol ?? throw new ArgumentNullException(nameof(parameterSymbol));
		}
	}
	public sealed class FunctionCallBoundExpression : IBoundExpression
	{
		public IType Type => CalledFunction.GetReturnType();
		public INode OriginalNode { get; }
		public FunctionSymbol CalledFunction { get; }
		public ImmutableArray<BoundCallArgument> Arguments;

		public FunctionCallBoundExpression(INode originalNode, FunctionSymbol calledFunction, ImmutableArray<BoundCallArgument> arguments)
		{
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
			CalledFunction = calledFunction ?? throw new ArgumentNullException(nameof(calledFunction));
			Arguments = arguments;
		}

		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Visit(this);
	}

	public sealed class FunctionBlockCallBoundExpression : IBoundExpression
	{
		public IType Type => CalledFunctionBlock.GetReturnType();
		public INode OriginalNode { get; }
		public FunctionBlockSymbol CalledFunctionBlock => (FunctionBlockSymbol)CalledInstance.Type;
		public IBoundExpression CalledInstance { get; }
		public ImmutableArray<BoundCallArgument> Arguments;

		public FunctionBlockCallBoundExpression(INode originalNode, IBoundExpression calledInstance, ImmutableArray<BoundCallArgument> arguments)
		{
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
			CalledInstance = calledInstance;
			Arguments = arguments;
		}

		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Visit(this);
	}

	public sealed class SequenceBoundStatement : IBoundStatement
	{
		public readonly ImmutableArray<IBoundStatement> Statements;

		public SequenceBoundStatement(ImmutableArray<IBoundStatement> statements)
		{
			Statements = statements;
		}

		public T Accept<T>(IBoundStatement.IVisitor<T> visitor) => visitor.Visit(this);
	}
	
	public sealed class ExpressionBoundStatement : IBoundStatement
	{
		public readonly IBoundExpression Expression;

		public ExpressionBoundStatement(IBoundExpression expression)
		{
			Expression = expression ?? throw new ArgumentNullException(nameof(expression));
		}

		public T Accept<T>(IBoundStatement.IVisitor<T> visitor) => visitor.Visit(this);
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

		public T Accept<T>(IBoundStatement.IVisitor<T> visitor) => visitor.Visit(this);
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

		T IBoundStatement.Accept<T>(IBoundStatement.IVisitor<T> visitor) => visitor.Visit(this);
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

		T IBoundStatement.Accept<T>(IBoundStatement.IVisitor<T> visitor) => visitor.Visit(this);
	}

	public sealed class ExitBoundStatement : IBoundStatement
	{
		T IBoundStatement.Accept<T>(IBoundStatement.IVisitor<T> visitor) => visitor.Visit(this);
	}
	public sealed class ContinueBoundStatement : IBoundStatement
	{
		T IBoundStatement.Accept<T>(IBoundStatement.IVisitor<T> visitor) => visitor.Visit(this);
	}
	public sealed class ReturnBoundStatement : IBoundStatement
	{
		T IBoundStatement.Accept<T>(IBoundStatement.IVisitor<T> visitor) => visitor.Visit(this);
	}
	public sealed class ForLoopBoundStatement : IBoundStatement
	{
		public readonly IBoundExpression Index;
		public readonly IBoundExpression Initial;
		public readonly IBoundExpression UpperBound;
		public readonly IBoundExpression Step;
		public readonly FunctionSymbol IncrementFunctionSymbol;
		public readonly IBoundStatement Body;

		public ForLoopBoundStatement(IBoundExpression index, IBoundExpression initial, IBoundExpression upperBound, IBoundExpression step, FunctionSymbol incrementFunctionSymbol, IBoundStatement body)
		{
			Index = index ?? throw new ArgumentNullException(nameof(index));
			Initial = initial ?? throw new ArgumentNullException(nameof(initial));
			UpperBound = upperBound ?? throw new ArgumentNullException(nameof(upperBound));
			Step = step ?? throw new ArgumentNullException(nameof(step));
			IncrementFunctionSymbol = incrementFunctionSymbol ?? throw new ArgumentNullException(nameof(incrementFunctionSymbol));
			Body = body ?? throw new ArgumentNullException(nameof(body));
		}

		T IBoundStatement.Accept<T>(IBoundStatement.IVisitor<T> visitor) => visitor.Visit(this);
	}
}
