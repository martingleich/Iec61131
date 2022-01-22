using System.Collections.Generic;
using System.Collections.Immutable;

namespace Compiler
{
	public abstract class VarDeclTreeNode
	{
		protected readonly List<InlineVarDeclTreeNode> _children = new();

		public InlineVarDeclTreeNode AddChild(StatementListSyntax statementListSyntax)
		{
			var variablesBuilder = ImmutableArray.CreateBuilder<InlineLocalVariableSymbol>();
			foreach (var st in statementListSyntax.Statements)
			{
				if (st is LocalVarDeclStatementSyntax localVarDecl)
				{
					var variable = new InlineLocalVariableSymbol(localVarDecl.SourceSpan, localVarDecl.Identifier);
					variablesBuilder.Add(variable);
				}
			}
			var variables = variablesBuilder.ToSymbolSetWithDuplicates(RootNode.Messages);
			return AddChild(variables);
		}

		public InlineVarDeclTreeNode AddChild(ForStatementDeclareLocalIndexSyntax declaredIndex)
		{
			var variable = new InlineLocalVariableSymbol(declaredIndex.SourceSpan, declaredIndex.Identifier);
			var variables = SymbolSet.Create(variable);
			return AddChild(variables);
		}

		private InlineVarDeclTreeNode AddChild(SymbolSet<InlineLocalVariableSymbol> variables)
		{
			var newChild = new InlineVarDeclTreeNode(variables, RootNode);
			_children.Add(newChild);
			return newChild;
		}

		protected abstract RootVarDeclTreeNode RootNode { get; }

		public virtual void GetAllTrackedVariables(ImmutableArray<FlowAnalyzer.TrackedVariable>.Builder builder)
		{
			foreach (var child in _children)
				child.GetAllTrackedVariables(builder);
		}
	}
}
