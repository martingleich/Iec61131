using Compiler.Messages;
using Compiler.Scopes;
using System;
using System.Collections.Immutable;

namespace Compiler
{
	public sealed class RootVarDeclTreeNode : VarDeclTreeNode
	{
		public readonly OrderedSymbolSet<LocalVariableSymbol> Locals;
		public readonly MessageBag Messages;
		private int _nextLocalId;

		public RootVarDeclTreeNode(OrderedSymbolSet<LocalVariableSymbol> locals, MessageBag messages, int firstId)
		{
			Locals = locals;
			Messages = messages;
			_nextLocalId = firstId;
		}

		private static readonly object? Marker = new();
		public static RootVarDeclTreeNode FromLocalsSyntax(SyntaxArray<VarDeclBlockSyntax> vardecls, IScope scope)
		{
			var messages = new MessageBag();
			int id = 0;
			var locals = ProjectBinder.BindVariableBlocks(vardecls, scope, messages,
			   kind => kind is VarTempToken ? Marker : null,
			   (_, scope, bag, syntax) =>
			   {
				   var type = TypeCompiler.MapComplete(scope, syntax.Type.Type, messages);
				   var initialValue = syntax.Initial != null ? ExpressionBinder.Bind(syntax.Initial.Value, scope, messages, type) : null;
				   return new LocalVariableSymbol(
					   syntax.TokenIdentifier.SourceSpan,
					   syntax.Identifier,
					   id++,
					   type,
					   initialValue);
			   }).ToOrderedSymbolSetWithDuplicates(messages);
			return new RootVarDeclTreeNode(locals, messages, id);
		}


		protected override RootVarDeclTreeNode RootNode => this;
		public void GetAllMessages(MessageBag messages, Func<IVariableSymbol, IVariableSymbol?> getAlreadyDeclared)
		{
			if (Messages != null)
				messages.AddRange(Messages);
			foreach (var local in Locals)
			{
				if (getAlreadyDeclared(local) is IVariableSymbol alreadyDeclared)
				{
					messages.Add(new SymbolAlreadyExistsMessage(local.Name, alreadyDeclared.DeclaringSpan, local.DeclaringSpan));
				}
			}
			IVariableSymbol? getShadowed(IVariableSymbol v) => Locals.TryGetValue(v.Name) ?? getAlreadyDeclared(v);
			foreach (var child in _children)
				child.GetAllMessages(messages, getShadowed);
		}

		public override void GetAllTrackedVariables(ImmutableArray<FlowAnalyzer.TrackedVariable>.Builder builder)
		{
			foreach (var local in Locals)
			{
				if (local.InitialValue == null)
					builder.Add(FlowAnalyzer.TrackedVariable.Unrequired(local));
			}
			base.GetAllTrackedVariables(builder);
		}

		public ImmutableArray<IBoundExpression> GetBeforeCodeInitializations()
		{
			var builder = ImmutableArray.CreateBuilder<IBoundExpression>();
			foreach (var local in Locals)
				if (local.InitialValue != null)
					builder.Add(local.InitialValue);
			return builder.ToImmutable();
		}
		public ImmutableDictionary<LocalVariableSymbol, IBoundExpression> GetInitialValues()
		{
			var builder = ImmutableDictionary.CreateBuilder<LocalVariableSymbol, IBoundExpression>(SymbolByNameComparer<LocalVariableSymbol>.Instance);
			foreach (var local in Locals)
				if (local.InitialValue != null)
					builder.Add(local, local.InitialValue);
			return builder.ToImmutable();
		}

		public IScope GetScope(IScope outerScope) => new TemporaryVariablesScope(outerScope, Locals);

		public int NextLocalId() => _nextLocalId++;

		private sealed class TemporaryVariablesScope : AInnerScope<IScope>
		{
			public readonly OrderedSymbolSet<LocalVariableSymbol> LocalVariables;
			public TemporaryVariablesScope(IScope outerScope, OrderedSymbolSet<LocalVariableSymbol> localVariables) : base(outerScope)
			{
				LocalVariables = localVariables;
			}

			public override ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourceSpan sourceSpan)
			{
				if (LocalVariables.TryGetValue(identifier, out var localVariableSymbol))
					return localVariableSymbol;
				return base.LookupVariable(identifier, sourceSpan);
			}
		}
	}
}
