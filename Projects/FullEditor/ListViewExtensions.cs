using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;

namespace FullEditor
{
	public static class ListViewExtensions
	{
		public static IEnumerable<ListViewItem> SelectedItems(this ListView view) => view.SelectedItems.Cast<ListViewItem>();
		public static IEnumerable<ListViewItem> Items(this ListView view) => view.Items.Cast<ListViewItem>();
		public static IEnumerable<ListViewItem.ListViewSubItem> SubItems(this ListViewItem item) => item.SubItems.Cast<ListViewItem.ListViewSubItem>();
	}
}
