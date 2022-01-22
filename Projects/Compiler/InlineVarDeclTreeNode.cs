using Compiler.Messages;
using Compiler.Scopes;
using System;
using System.Collections.Immutable;

namespace Compiler
{
	public sealed class InlineVarDeclTreeNode : VarDeclTreeNode
	{
		private readonly SymbolSet<InlineLocalVariableSymbol> _variables;
		protected override RootVarDeclTreeNode RootNode { get; }
		public override void GetAllTrackedVariables(ImmutableArray<FlowAnalyzer.TrackedVariable>.Builder builder)
		{
			foreach (var local in _variables)
				builder.Add(FlowAnalyzer.TrackedVariable.Inline(local));
			base.GetAllTrackedVariables(builder);
		}

		public InlineVarDeclTreeNode(
			SymbolSet<InlineLocalVariableSymbol> variables,
			RootVarDeclTreeNode rootNode)
		{
			_variables = variables;
			RootNode = rootNode;
		}

		public IStatementScope GetScope(IStatementScope externalScope)
		{
			if (_variables.Count == 0)
				return externalScope;
			return new TemporaryVariablesScope(externalScope, _variables);
		}

		public void GetAllMessages(MessageBag messages, Func<IVariableSymbol, IVariableSymbol?> getShadowed)
		{
			foreach (var variable in _variables)
			{
				var shadowed = getShadowed(variable);
				if (shadowed != null)
					messages.Add(new ShadowedLocalVariableMessage(variable.DeclaringSpan, variable, shadowed));
			}
			IVariableSymbol? ChildGetShadowed(IVariableSymbol v) => getShadowed(v) ?? _variables.TryGetValue(v.Name);
			foreach (var child in _children)
				child.GetAllMessages(messages, ChildGetShadowed);
		}

		private sealed class TemporaryVariablesScope : AInnerStatementScope<IStatementScope>
		{
			public readonly SymbolSet<InlineLocalVariableSymbol> LocalVariables;
			public TemporaryVariablesScope(IStatementScope outerScope, SymbolSet<InlineLocalVariableSymbol> localVariables) : base(outerScope)
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

		public InlineLocalVariableSymbol GetVarFor(LocalVarDeclStatementSyntax localVarDeclStatementSyntax)
			=> _variables[localVarDeclStatementSyntax.Identifier];
		public InlineLocalVariableSymbol GetVarFor(ForStatementDeclareLocalIndexSyntax declaredIndex)
			=> _variables[declaredIndex.Identifier];
	}
}
