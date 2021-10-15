﻿using System;
using Compiler.Messages;
using Compiler.Types;

namespace Compiler
{
	public sealed partial class ExpressionBinder
	{
		private abstract class BoundCompoLeft
		{
			public sealed class Expression : BoundCompoLeft
			{
				private readonly IBoundExpression boundLeft;

				public Expression(IBoundExpression boundLeft)
				{
					this.boundLeft = boundLeft ?? throw new ArgumentNullException(nameof(boundLeft));
				}

				public override IBoundExpression BindCompo(ISyntax originalNode, IdentifierToken name, IType? context, ExpressionBinder binder)
				{
					if (!(boundLeft.Type is StructuredTypeSymbol structuredType && structuredType.Fields.TryGetValue(name.Value.ToCaseInsensitive(), out var field)))
					{
						if (!boundLeft.Type.IsError())
							binder.MessageBag.Add(new FieldNotFoundMessage(boundLeft.Type, name.Value.ToCaseInsensitive(), name.SourcePosition));
						field = new FieldVariableSymbol(
							name.SourcePosition,
							name.Value.ToCaseInsensitive(),
							boundLeft.Type);
					}

					return binder.ImplicitCast(new FieldAccessBoundExpression(originalNode, boundLeft, field), context);
				}
			}
			public sealed class Type : BoundCompoLeft
			{
				private readonly IType _type;
				private readonly IType _resolvedType;

				public Type(IType type, IType resolvedType)
				{
					_type = type ?? throw new ArgumentNullException(nameof(type));
					_resolvedType = resolvedType ?? throw new ArgumentNullException(nameof(resolvedType));
				}

				public override IBoundExpression BindCompo(ISyntax originalNode, IdentifierToken name, IType? context, ExpressionBinder binder)
				{
					if (_resolvedType is EnumTypeSymbol enumTypeSymbol)
					{
						if (enumTypeSymbol.Values.TryGetValue(name.Value.ToCaseInsensitive(), out var enumValue))
						{
							return new LiteralBoundExpression(originalNode, enumValue._GetConstantValue(binder.MessageBag));
						}
						else
						{
							binder.MessageBag.Add(new EnumValueNotFoundMessage(enumTypeSymbol, name.Value.ToCaseInsensitive(), name.SourcePosition));
							return new LiteralBoundExpression(originalNode,
								new EnumLiteralValue(enumTypeSymbol, new UnknownLiteralValue(enumTypeSymbol.BaseType)));
						}
					}
					else
					{
						binder.MessageBag.Add(new TypeDoesNotContainStaticVariableMessage(_type, name.Value.ToCaseInsensitive(), name.SourcePosition));
						return new StaticVariableBoundExpression(originalNode, GlobalVariableSymbol.CreateError(name.SourcePosition, name.Value.ToCaseInsensitive()));
					}
				}
			}
			public sealed class Gvl : BoundCompoLeft
			{
				private readonly GlobalVariableListSymbol _gvl;

				public Gvl(GlobalVariableListSymbol gvl)
				{
					_gvl = gvl ?? throw new ArgumentNullException(nameof(gvl));
				}

				public override IBoundExpression BindCompo(ISyntax originalNode, IdentifierToken name, IType? context, ExpressionBinder binder)
				{
					var varName = name.Value.ToCaseInsensitive();
					var varPos = name.SourcePosition;
					if (!_gvl.Variables.TryGetValue(varName, out var globalVariable))
					{
						binder.MessageBag.Add(new GlobalVariableNotFoundMessage(_gvl, varName, varPos));
						globalVariable = GlobalVariableSymbol.CreateError(varPos, varName);
					}

					return binder.ImplicitCast(new StaticVariableBoundExpression(originalNode, globalVariable), context);
				}
			}

			public abstract IBoundExpression BindCompo(ISyntax originalNode, IdentifierToken name, IType? context, ExpressionBinder binder);
		}
	
		private BoundCompoLeft BindLeftCompo(IExpressionSyntax syntax)
		{
			if (syntax is VariableExpressionSyntax variableExpressionSyntax)
			{
				var name = variableExpressionSyntax.Identifier.ToCaseInsensitive();
				var pos = variableExpressionSyntax.SourcePosition;
				var maybeVar = Scope.LookupVariable(name, pos);
				if (!maybeVar.HasErrors)
					return new BoundCompoLeft.Expression(new VariableBoundExpression(syntax, maybeVar.Value));
				var maybeType = Scope.LookupType(name, pos);
				if (!maybeType.HasErrors)
				{
					var resolved = TypeRelations.ResolveAlias(maybeType.Value);
					return new BoundCompoLeft.Type(maybeType.Value, resolved);
				}
				var maybeGvl = Scope.LookupGlobalVariableList(name, pos);
				if(!maybeGvl.HasErrors)
					return new BoundCompoLeft.Gvl(maybeGvl.Value);
				MessageBag.Add(ExpectedVariableOrTypeOrGvlMessage.Create(variableExpressionSyntax));
				return new BoundCompoLeft.Expression(new VariableBoundExpression(
					syntax,
					IVariableSymbol.CreateError(pos, name)));
			}
			var expr = syntax.Accept(this, null);
			return new BoundCompoLeft.Expression(expr);
		}
	}
}
