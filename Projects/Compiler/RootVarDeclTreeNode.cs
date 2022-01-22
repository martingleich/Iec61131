using Compiler.Messages;
using Compiler.Scopes;
using System;
using System.Collections.Immutable;

namespace Compiler
{
	public sealed class RootVarDeclTreeNode : VarDeclTreeNode
	{
		private readonly OrderedSymbolSet<LocalVariableSymbol> Locals;
		private MessageBag? _messages;

		public RootVarDeclTreeNode(OrderedSymbolSet<LocalVariableSymbol> locals)
		{
			Locals = locals;
		}

		public MessageBag Messages
		{
			get
			{
				if (_messages == null)
					_messages = new MessageBag();
				return _messages;
			}
		}

		protected override RootVarDeclTreeNode RootNode => this;
		public void GetAllMessages(MessageBag messages, Func<IVariableSymbol, IVariableSymbol?> getAlreadyDeclared)
		{
			if (_messages != null)
				messages.AddRange(_messages);
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

		public IScope GetScope(IScope outerScope) => new TemporaryVariablesScope(outerScope, Locals);

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
