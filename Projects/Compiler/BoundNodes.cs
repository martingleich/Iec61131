using Compiler.Types;
using System;
using System.Collections.Immutable;

namespace Compiler
{
	public interface IBoundNode
	{
		INode OriginalNode { get; }
	}
	public interface IBoundExpression : IBoundNode
	{
		IType Type { get; }

		T Accept<T>(IVisitor<T> visitor);
		T Accept<T, TContext>(IVisitor<T, TContext> visitor, TContext context);

		public interface IVisitor<T>
		{
			T Visit(LiteralBoundExpression literalBoundExpression);
			T Visit(SizeOfTypeBoundExpression sizeOfTypeBoundExpression);
			T Visit(VariableBoundExpression variableBoundExpression);
			T Visit(ImplicitEnumToBaseTypeCastBoundExpression implicitEnumCastBoundExpression);
			T Visit(BinaryOperatorBoundExpression binaryOperatorBoundExpression);
			T Visit(ImplicitPointerTypeCastBoundExpression implicitPointerTypeCaseBoundExpression);
			T Visit(ImplicitCastBoundExpression implicitArithmeticCaseBoundExpression);
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
			T Visit(ImplicitDiscardBoundExpression implicitDiscardBoundExpression);
			T Visit(CallBoundExpression callBoundExpression);
			T Visit(InitializerBoundExpression initializerBoundExpression);
		}
		public interface IVisitor<T, TContext>
		{
			T Visit(LiteralBoundExpression literalBoundExpression, TContext context);
			T Visit(SizeOfTypeBoundExpression sizeOfTypeBoundExpression, TContext context);
			T Visit(VariableBoundExpression variableBoundExpression, TContext context);
			T Visit(ImplicitEnumToBaseTypeCastBoundExpression implicitEnumCastBoundExpression, TContext context);
			T Visit(BinaryOperatorBoundExpression binaryOperatorBoundExpression, TContext context);
			T Visit(ImplicitPointerTypeCastBoundExpression implicitPointerTypeCaseBoundExpression, TContext context);
			T Visit(ImplicitCastBoundExpression implicitArithmeticCaseBoundExpression, TContext context);
			T Visit(UnaryOperatorBoundExpression unaryOperatorBoundExpression, TContext context);
			T Visit(PointerDiffrenceBoundExpression pointerDiffrenceBoundExpression, TContext context);
			T Visit(PointerOffsetBoundExpression pointerOffsetBoundExpression, TContext context);
			T Visit(DerefBoundExpression derefBoundExpression, TContext context);
			T Visit(ImplicitAliasToBaseTypeCastBoundExpression aliasToBaseTypeCastBoundExpression, TContext context);
			T Visit(ImplicitErrorCastBoundExpression implicitErrorCastBoundExpression, TContext context);
			T Visit(ImplicitAliasFromBaseTypeCastBoundExpression implicitAliasFromBaseTypeCastBoundExpression, TContext context);
			T Visit(ArrayIndexAccessBoundExpression arrayIndexAccessBoundExpression, TContext context);
			T Visit(PointerIndexAccessBoundExpression pointerIndexAccessBoundExpression, TContext context);
			T Visit(FieldAccessBoundExpression fieldAccessBoundExpression, TContext context);
			T Visit(ImplicitDiscardBoundExpression implicitDiscardBoundExpression, TContext context);
			T Visit(CallBoundExpression callBoundExpression, TContext context);
			T Visit(InitializerBoundExpression initializerBoundExpression, TContext context);
		}
	}

	public interface IBoundStatement : IBoundNode
	{
		void Accept(IVisitor visitor);
		T Accept<T>(IVisitor<T> visitor);
		T Accept<T, TContext>(IVisitor<T, TContext> visitor, TContext context);
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
			T Visit(InitVariableBoundStatement initVariableBoundStatement);
		}
		interface IVisitor
		{
			void Visit(SequenceBoundStatement sequenceBoundStatement);
			void Visit(ExpressionBoundStatement expressionBoundStatement);
			void Visit(AssignBoundStatement assignToExpressionBoundStatement);
			void Visit(IfBoundStatement ifBoundStatement);
			void Visit(WhileBoundStatement whileBoundStatement);
			void Visit(ExitBoundStatement exitBoundStatement);
			void Visit(ContinueBoundStatement continueBoundStatement);
			void Visit(ReturnBoundStatement returnBoundStatement);
			void Visit(ForLoopBoundStatement forLoopBoundStatement);
			void Visit(InitVariableBoundStatement initVariableBoundStatement);
		}
		interface IVisitor<T, TContext>
		{
			T Visit(SequenceBoundStatement sequenceBoundStatement, TContext context);
			T Visit(ExpressionBoundStatement expressionBoundStatement, TContext context);
			T Visit(AssignBoundStatement assignToExpressionBoundStatement, TContext context);
			T Visit(IfBoundStatement ifBoundStatement, TContext context);
			T Visit(WhileBoundStatement whileBoundStatement, TContext context);
			T Visit(ExitBoundStatement exitBoundStatement, TContext context);
			T Visit(ContinueBoundStatement continueBoundStatement, TContext context);
			T Visit(ReturnBoundStatement returnBoundStatement, TContext context);
			T Visit(ForLoopBoundStatement forLoopBoundStatement, TContext context);
			T Visit(InitVariableBoundStatement initVariableBoundStatement, TContext context);
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
		T IBoundExpression.Accept<T, TContext>(IBoundExpression.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
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
		T IBoundExpression.Accept<T, TContext>(IBoundExpression.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
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
		T IBoundExpression.Accept<T, TContext>(IBoundExpression.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
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
		T IBoundExpression.Accept<T, TContext>(IBoundExpression.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
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
		T IBoundExpression.Accept<T, TContext>(IBoundExpression.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
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
		T IBoundExpression.Accept<T, TContext>(IBoundExpression.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
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
		T IBoundExpression.Accept<T, TContext>(IBoundExpression.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
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
		T IBoundExpression.Accept<T, TContext>(IBoundExpression.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
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
		T IBoundExpression.Accept<T, TContext>(IBoundExpression.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
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
		T IBoundExpression.Accept<T, TContext>(IBoundExpression.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
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
		T IBoundExpression.Accept<T, TContext>(IBoundExpression.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
	}

	public sealed class ImplicitCastBoundExpression : IBoundExpression
	{
		public readonly IBoundExpression Value;
		public INode OriginalNode => Value.OriginalNode;

		public ImplicitCastBoundExpression(IBoundExpression value, FunctionVariableSymbol castFunction)
		{
			Value = value ?? throw new ArgumentNullException(nameof(value));
			CastFunction = castFunction ?? throw new ArgumentNullException(nameof(castFunction));
			Type = CastFunction.Type.GetReturnType();
		}

		public FunctionVariableSymbol CastFunction { get; }
		public IType Type { get; }
		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Visit(this);
		T IBoundExpression.Accept<T, TContext>(IBoundExpression.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
	}
	public sealed class BinaryOperatorBoundExpression : IBoundExpression
	{
		public INode OriginalNode { get; }
		public IType Type { get; }
		public readonly IBoundExpression Left;
		public readonly IBoundExpression Right;
		public readonly FunctionVariableSymbol Function;

		public BinaryOperatorBoundExpression(INode originalNode, IType type, IBoundExpression left, IBoundExpression right, FunctionVariableSymbol function)
		{
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
			Type = type ?? throw new ArgumentNullException(nameof(type));
			Left = left ?? throw new ArgumentNullException(nameof(left));
			Right = right ?? throw new ArgumentNullException(nameof(right));
			Function = function ?? throw new ArgumentNullException(nameof(function));
		}

		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Visit(this);
		T IBoundExpression.Accept<T, TContext>(IBoundExpression.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
	}
	public sealed class UnaryOperatorBoundExpression : IBoundExpression
	{
		public INode OriginalNode { get; }
		public IType Type { get; }
		public readonly IBoundExpression Value;
		public readonly FunctionVariableSymbol Function;

		public UnaryOperatorBoundExpression(INode originalNode, IType type, IBoundExpression value, FunctionVariableSymbol function)
		{
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
			Type = type ?? throw new ArgumentNullException(nameof(type));
			Value = value ?? throw new ArgumentNullException(nameof(value));
			Function = function ?? throw new ArgumentNullException(nameof(function));
		}

		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Visit(this);
		T IBoundExpression.Accept<T, TContext>(IBoundExpression.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
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
		T IBoundExpression.Accept<T, TContext>(IBoundExpression.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
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
		T IBoundExpression.Accept<T, TContext>(IBoundExpression.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
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
		T IBoundExpression.Accept<T, TContext>(IBoundExpression.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
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
		T IBoundExpression.Accept<T, TContext>(IBoundExpression.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
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
	public sealed class CallBoundExpression : IBoundExpression
	{
		public IType Type { get; }
		public INode OriginalNode { get; }
		public readonly IBoundExpression Callee;
		public readonly ImmutableArray<BoundCallArgument> Arguments;

		public CallBoundExpression(INode originalNode, IBoundExpression callee, ImmutableArray<BoundCallArgument> arguments, IType type)
		{
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
			Callee = callee ?? throw new ArgumentNullException(nameof(callee));
			Arguments = arguments;
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Visit(this);
		T IBoundExpression.Accept<T, TContext>(IBoundExpression.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
	}
	public sealed class BoundConstantIntegerValue
	{
		public IBoundExpression? Expression;
		public int Value;

		public BoundConstantIntegerValue(IBoundExpression? expression, int value)
		{
			Expression = expression;
			Value = value;
		}
		public override string ToString() => Value.ToString();
	}
	public sealed class InitializerBoundExpression : IBoundExpression
	{
		public abstract class ABoundElement
		{
			public readonly IBoundExpression Value;

			protected ABoundElement(IBoundExpression value)
			{
				Value = value ?? throw new ArgumentNullException(nameof(value));
			}

			public sealed class ArrayElement : ABoundElement
			{
				public readonly BoundConstantIntegerValue Index;

				public ArrayElement(BoundConstantIntegerValue index, IBoundExpression value) : base(value)
				{
					Index = index ?? throw new ArgumentNullException(nameof(index));
				}

				public override string ToString() => $"[{Index}] := {Value}";
			}
			public sealed class AllElements : ABoundElement
			{
				public AllElements(IBoundExpression value) : base(value)
				{
				}
			}
			public sealed class FieldElement : ABoundElement
			{
				public readonly FieldVariableSymbol Field;
				public FieldElement(FieldVariableSymbol field, IBoundExpression value) : base(value)
				{
					Field = field ?? throw new ArgumentNullException(nameof(field));
				}
				public override string ToString() => $".{Field.Name} := {Value}";
			}
			public sealed class UnknownElement : ABoundElement
			{
				public UnknownElement(IBoundExpression value) : base(value)
				{
				}
			}
		}

		public ImmutableArray<ABoundElement> Elements;

		public InitializerBoundExpression(ImmutableArray<ABoundElement> elements, IType type, INode originalNode)
		{
			Elements = elements;
			Type = type ?? throw new ArgumentNullException(nameof(type));
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
		}

		public IType Type { get; }
		public INode OriginalNode { get; }

		public T Accept<T>(IBoundExpression.IVisitor<T> visitor) => visitor.Visit(this);
		T IBoundExpression.Accept<T, TContext>(IBoundExpression.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);

		public override string ToString() => $"{Type.Code}#{{{string.Join(", ", Elements)}}}";
	}
	
	public sealed class SequenceBoundStatement : IBoundStatement
	{
		public INode OriginalNode { get; }
		public readonly ImmutableArray<IBoundStatement> Statements;

		public SequenceBoundStatement(INode originalNode, ImmutableArray<IBoundStatement> statements)
		{
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
			Statements = statements;
		}

		void IBoundStatement.Accept(IBoundStatement.IVisitor visitor) => visitor.Visit(this);
		T IBoundStatement.Accept<T>(IBoundStatement.IVisitor<T> visitor) => visitor.Visit(this);
		T IBoundStatement.Accept<T, TContext>(IBoundStatement.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
	}
	
	public sealed class ExpressionBoundStatement : IBoundStatement
	{
		public INode OriginalNode { get; }
		public readonly IBoundExpression Expression;

		public ExpressionBoundStatement(INode originalNode, IBoundExpression expression)
		{
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
			Expression = expression ?? throw new ArgumentNullException(nameof(expression));
		}

		void IBoundStatement.Accept(IBoundStatement.IVisitor visitor) => visitor.Visit(this);
		T IBoundStatement.Accept<T>(IBoundStatement.IVisitor<T> visitor) => visitor.Visit(this);
		T IBoundStatement.Accept<T, TContext>(IBoundStatement.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
	}

	public sealed class AssignBoundStatement : IBoundStatement
	{
		public INode OriginalNode { get; }
		public readonly IBoundExpression LeftSide;
		public readonly IBoundExpression RightSide;

		public AssignBoundStatement(INode originalNode, IBoundExpression leftSide, IBoundExpression rightSide)
		{
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
			LeftSide = leftSide ?? throw new ArgumentNullException(nameof(leftSide));
			RightSide = rightSide ?? throw new ArgumentNullException(nameof(rightSide));
		}

		void IBoundStatement.Accept(IBoundStatement.IVisitor visitor) => visitor.Visit(this);
		public T Accept<T>(IBoundStatement.IVisitor<T> visitor) => visitor.Visit(this);
		T IBoundStatement.Accept<T, TContext>(IBoundStatement.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
	}
	public sealed class InitVariableBoundStatement : IBoundStatement
	{
		public INode OriginalNode { get; }
		public readonly IVariableSymbol LeftSide;
		public readonly IBoundExpression? RightSide;

		public InitVariableBoundStatement(INode originalNode, IVariableSymbol leftSide, IBoundExpression? rightSide)
		{
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
			LeftSide = leftSide ?? throw new ArgumentNullException(nameof(leftSide));
			RightSide = rightSide;
		}

		void IBoundStatement.Accept(IBoundStatement.IVisitor visitor) => visitor.Visit(this);
		T IBoundStatement.Accept<T>(IBoundStatement.IVisitor<T> visitor) => visitor.Visit(this);
		T IBoundStatement.Accept<T, TContext>(IBoundStatement.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
	}

	public sealed class IfBoundStatement : IBoundStatement
	{
		public INode OriginalNode { get; }
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

		public IfBoundStatement(INode originalNode, ImmutableArray<Branch> branches)
		{
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
			Branches = branches;
		}

		void IBoundStatement.Accept(IBoundStatement.IVisitor visitor) => visitor.Visit(this);
		T IBoundStatement.Accept<T>(IBoundStatement.IVisitor<T> visitor) => visitor.Visit(this);
		T IBoundStatement.Accept<T, TContext>(IBoundStatement.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
	}

	public sealed class WhileBoundStatement : IBoundStatement
	{
		public INode OriginalNode { get; }
		public readonly IBoundExpression Condition;
		public readonly IBoundStatement Body;

		public WhileBoundStatement(INode originalNode, IBoundExpression condition, IBoundStatement body)
		{
			Condition = condition ?? throw new ArgumentNullException(nameof(condition));
			Body = body ?? throw new ArgumentNullException(nameof(body));
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
		}

		void IBoundStatement.Accept(IBoundStatement.IVisitor visitor) => visitor.Visit(this);
		T IBoundStatement.Accept<T>(IBoundStatement.IVisitor<T> visitor) => visitor.Visit(this);
		T IBoundStatement.Accept<T, TContext>(IBoundStatement.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
	}

	public sealed class ExitBoundStatement : IBoundStatement
	{
		public INode OriginalNode { get; }

		public ExitBoundStatement(INode originalNode)
		{
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
		}

		void IBoundStatement.Accept(IBoundStatement.IVisitor visitor) => visitor.Visit(this);
		T IBoundStatement.Accept<T>(IBoundStatement.IVisitor<T> visitor) => visitor.Visit(this);
		T IBoundStatement.Accept<T, TContext>(IBoundStatement.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
	}
	public sealed class ContinueBoundStatement : IBoundStatement
	{
		public INode OriginalNode { get; }

		public ContinueBoundStatement(INode originalNode)
		{
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
		}

		void IBoundStatement.Accept(IBoundStatement.IVisitor visitor) => visitor.Visit(this);
		T IBoundStatement.Accept<T>(IBoundStatement.IVisitor<T> visitor) => visitor.Visit(this);
		T IBoundStatement.Accept<T, TContext>(IBoundStatement.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
	}
	public sealed class ReturnBoundStatement : IBoundStatement
	{
		public ReturnBoundStatement(INode originalNode)
		{
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
		}

		public INode OriginalNode { get; }
		void IBoundStatement.Accept(IBoundStatement.IVisitor visitor) => visitor.Visit(this);
		T IBoundStatement.Accept<T>(IBoundStatement.IVisitor<T> visitor) => visitor.Visit(this);
		T IBoundStatement.Accept<T, TContext>(IBoundStatement.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
	}
	public sealed class ForLoopFunctions
	{
		public readonly FunctionVariableSymbol Add;
		public readonly FunctionVariableSymbol LessEqual;

		public ForLoopFunctions(FunctionVariableSymbol add, FunctionVariableSymbol lessEqual)
		{
			Add = add ?? throw new ArgumentNullException(nameof(add));
			LessEqual = lessEqual ?? throw new ArgumentNullException(nameof(lessEqual));
		}
	}
	public sealed class ForLoopBoundStatement : IBoundStatement
	{
		public INode OriginalNode { get; }

		public readonly IBoundExpression Index;
		public readonly IBoundExpression Initial;
		public readonly IBoundExpression UpperBound;
		public readonly IBoundExpression Step;
		public readonly IBoundStatement Body;
		public readonly ForLoopFunctions? Functions;

		public ForLoopBoundStatement(INode originalNode,
							   IBoundExpression index,
							   IBoundExpression initial,
							   IBoundExpression upperBound,
							   IBoundExpression step,
							   ForLoopFunctions? functions,
							   IBoundStatement body)
		{
			OriginalNode = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
			Index = index ?? throw new ArgumentNullException(nameof(index));
			Initial = initial ?? throw new ArgumentNullException(nameof(initial));
			UpperBound = upperBound ?? throw new ArgumentNullException(nameof(upperBound));
			Step = step ?? throw new ArgumentNullException(nameof(step));
			Functions = functions;
			Body = body ?? throw new ArgumentNullException(nameof(body));
		}

		void IBoundStatement.Accept(IBoundStatement.IVisitor visitor) => visitor.Visit(this);
		T IBoundStatement.Accept<T>(IBoundStatement.IVisitor<T> visitor) => visitor.Visit(this);
		T IBoundStatement.Accept<T, TContext>(IBoundStatement.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
	}
}
