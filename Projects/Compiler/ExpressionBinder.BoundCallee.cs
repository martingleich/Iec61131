using Compiler.Messages;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Compiler
{
	public sealed partial class ExpressionBinder
	{
		private abstract class BoundCallee
		{
			public sealed class FunctionBlock : BoundCallee
			{
				private readonly IBoundExpression _expression;

				public FunctionBlock(IBoundExpression expression)
				{
					_expression = expression ?? throw new ArgumentNullException(nameof(expression));
				}

				public override IBoundExpression BindCall(ISyntax originalNode, SyntaxCommaSeparated<CallArgumentSyntax> arguments, ExpressionBinder expressionBinder)
				{
					if (_expression.Type is FunctionBlockSymbol fbSymbol)
					{
						var boundArgs = expressionBinder.BindFunctionCall(fbSymbol, false, arguments);
						return new FunctionBlockCallBoundExpression(originalNode, _expression, boundArgs);
					}
					else
					{
						expressionBinder.MessageBag.Add(new CannotCallTypeMessage(_expression.Type, originalNode.SourcePosition));
						return new FunctionBlockCallBoundExpression(originalNode, _expression, ImmutableArray<BoundCallArgument>.Empty);
					}
				}
			}
			public sealed class Function : BoundCallee
			{
				private readonly FunctionSymbol _function;

				public Function(FunctionSymbol function)
				{
					_function = function ?? throw new ArgumentNullException(nameof(function));
				}

				public override IBoundExpression BindCall(ISyntax originalNode, SyntaxCommaSeparated<CallArgumentSyntax> arguments, ExpressionBinder expressionBinder)
				{
					var boundArgs = expressionBinder.BindFunctionCall(_function, _function.IsError, arguments);
					return new FunctionCallBoundExpression(originalNode, _function, boundArgs);
				}
			}
			public abstract IBoundExpression BindCall(ISyntax originalNode, SyntaxCommaSeparated<CallArgumentSyntax> arguments, ExpressionBinder expressionBinder);
		}

		private BoundCallee BindCallee(IExpressionSyntax syntax)
		{
			if (syntax is VariableExpressionSyntax variableSyntax)
			{
				var maybeVar = Scope.LookupVariable(variableSyntax.Identifier, variableSyntax.TokenIdentifier.SourcePosition);
				if (!maybeVar.HasErrors)
					return new BoundCallee.FunctionBlock(new VariableBoundExpression(syntax, maybeVar.Value));
				var maybeFunction = Scope.LookupFunction(variableSyntax.Identifier, variableSyntax.TokenIdentifier.SourcePosition);
				if (!maybeFunction.HasErrors)
					return new BoundCallee.Function(maybeFunction.Value);
				MessageBag.Add(ExpectedVariableOrFunctionMessage.Create(variableSyntax));
				return new BoundCallee.Function(FunctionSymbol.CreateError(variableSyntax.SourcePosition));
			}
			else
			{
				var boundExpression = syntax.Accept(this, null);
				return new BoundCallee.FunctionBlock(boundExpression);
			}
		}

		private ImmutableArray<BoundCallArgument> BindFunctionCall(
			ICallableSymbol function,
			bool isError,
			SyntaxCommaSeparated<CallArgumentSyntax> args)
		{
			// Bind arguments
			if (args.Count != function.GetParameterCountWithoutReturn())
			{
				// Error wrong number of arguments.
				MessageBag.Add(!isError, new WrongNumberOfArgumentsMessage(function, args.Count, args.SourcePosition));
			}

			var boundArguments = ImmutableArray.CreateBuilder<BoundCallArgument>();
			int nextParamId = 0;
			bool afterExplicit = false;
			foreach (var arg in args)
			{
				ParameterVariableSymbol symbol;
				//	Find assigned argument
				if (arg.ExplicitParameter is ExplicitCallParameterSyntax explicitParameterSyntax)
				{
					if (!function.Parameters.TryGetValue(explicitParameterSyntax.Identifier, out var explicitParameter) ||
						explicitParameterSyntax.Identifier == function.Name) // The implicit output for return is not accessable from the outside
					{
						MessageBag.Add(!isError, new ParameterNotFoundMessage(function, explicitParameterSyntax.Identifier, explicitParameterSyntax.TokenIdentifier.SourcePosition));
						symbol = ParameterVariableSymbol.CreateError(explicitParameterSyntax.Identifier, explicitParameterSyntax.TokenIdentifier.SourcePosition);
					}
					else
					{
						symbol = explicitParameter;
						if (!explicitParameter.Kind.MatchesAssignKind(explicitParameterSyntax.ParameterKind))
							MessageBag.Add(new ParameterKindDoesNotMatchAssignMessage(symbol, explicitParameterSyntax.ParameterKind));
					}

					afterExplicit = true;
				}
				else
				{
					if (afterExplicit)
					{
						MessageBag.Add(new CannotUsePositionalParameterAfterExplicitMessage(arg.SourcePosition));
						symbol = ParameterVariableSymbol.CreateError(nextParamId, arg.SourcePosition);
					}
					else if (nextParamId >= function.Parameters.Length)
					{
						symbol = ParameterVariableSymbol.CreateError(nextParamId, arg.SourcePosition);
					}
					else
					{
						symbol = function.Parameters[nextParamId];
					}

					if (!symbol.Kind.Equals(ParameterKind.Input))
						MessageBag.Add(new NonInputParameterMustBePassedExplicit(symbol, arg.SourcePosition));

					++nextParamId;
				}

				boundArguments.Add(BindCallArgument(symbol, arg.Value));
			}

			var boundArgumentsFrozen = boundArguments.ToImmutable();
			CheckForDuplicateParameter(boundArgumentsFrozen);
			return boundArgumentsFrozen;

			void CheckForDuplicateParameter(ImmutableArray<BoundCallArgument> boundArguments)
			{
				foreach (var d in from arg in boundArguments
								  group arg by arg.ParameterSymbol.Name into duplicates
								  where duplicates.MoreThan(1)
								  select duplicates)
				{
					var first = d.First();
					foreach (var d2 in d.Skip(1))
					{
						MessageBag.Add(new ParameterWasAlreadyPassedMessage(first.ParameterSymbol, first.Parameter.OriginalNode.SourcePosition, d2.Parameter.OriginalNode.SourcePosition));
					}
				}
			}

			BoundCallArgument BindCallArgument(ParameterVariableSymbol symbol, IExpressionSyntax arg)
			{
				if (symbol.Kind.Equals(ParameterKind.Input))
				{
					var value = arg.Accept(this, symbol.Type);
					return new(
						symbol,
						new VariableBoundExpression(arg, symbol),
						value);
				}
				else if (symbol.Kind.Equals(ParameterKind.Output))
				{
					// The argument must be assignable.
					// The parameter type must be convertiable to the argument type.
					var boundArg = arg.Accept(this, null);
					var castedParameter = ImplicitCast(new VariableBoundExpression(arg, symbol), boundArg.Type);
					IsLValueChecker.IsLValue(boundArg).Extract(MessageBag);
					return new(
						symbol,
						castedParameter,
						boundArg);
				}
				else if (symbol.Kind.Equals(ParameterKind.InOut))
				{
					// The argument must be assignable
					// The type must be identical
					var boundArg = arg.Accept(this, null);
					var boundParameter = new VariableBoundExpression(arg, symbol);
					if (!TypeRelations.IsIdentical(boundArg.Type, boundParameter.Type))
						MessageBag.Add(new InoutArgumentMustHaveSameTypeMessage(boundArg.Type, boundParameter.Type, arg.SourcePosition));
					IsLValueChecker.IsLValue(boundArg).Extract(MessageBag);
					return new(
						symbol,
						boundParameter,
						boundArg);
				}
				else
				{
					throw new ArgumentException($"Unkown parameter kind '{symbol.Kind}'");
				}
			}
		}
	}
}
