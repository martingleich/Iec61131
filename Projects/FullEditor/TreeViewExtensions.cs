using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace FullEditor
{
	public static class TreeViewExtensions
	{
		public static TreeNode? FindNode(this TreeNodeCollection treeNodeCollection, object tag)
		{
			for (int i = 0; i < treeNodeCollection.Count; ++i)
			{
				if (treeNodeCollection[i].Tag.Equals(tag))
					return treeNodeCollection[i];
			}
			return null;
		}
		public static IEnumerable<TreeNode> TraverseDepthFirst(this TreeView view)
		{
#nullable disable
			foreach (TreeNode node in view.Nodes)
			{
				yield return node;
				foreach (TreeNode c in node.Nodes)
					foreach (var cc in TraverseDepthFirst(c))
						yield return cc;
			}
#nullable restore   
		}
		public static IEnumerable<TreeNode> TraverseDepthFirst(this TreeNode node)
		{
#nullable disable
			yield return node;
			foreach (TreeNode c in node.Nodes)
				foreach (var cc in TraverseDepthFirst(c))
					yield return cc;
#nullable restore   
		}
	}
}
