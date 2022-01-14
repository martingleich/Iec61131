using SyntaxEditor;
using System;
using System.Collections.Immutable;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Reactive.Linq;

namespace FullEditor
{
	public class MessageView : ListView
	{
		public MessageView(IObservable<ImmutableArray<ProjectMessage>> onNewMessageSet, SyntaxEditorControl syntaxEditor)
		{
			EditorView = syntaxEditor;
			OwnerDraw = true;

			SmallImageList = new ImageList();
			SmallImageList.Images.Add(MessageType.Error.ToString(), Resources.Images.Error);
			SmallImageList.Images.Add(MessageType.Warning.ToString(), Resources.Images.Warning);

			FullRowSelect = true;
			View = View.Details;
			MultiSelect = true;

			this.HeaderStyle = ColumnHeaderStyle.None;
			var iconHeader = Columns.Add("");
			iconHeader.Width = -2;
			var descriptionHeader = Columns.Add("");
			descriptionHeader.Width = -2;

			onNewMessageSet.ObserveOn(this).Subscribe(OnNewMessageSet);
			OnNewMessageSet(ImmutableArray<ProjectMessage>.Empty);
		}

		protected override void OnDrawColumnHeader(DrawListViewColumnHeaderEventArgs e)
		{
			base.OnDrawColumnHeader(e);
			e.DrawText();
		}
		protected override void OnDrawSubItem(DrawListViewSubItemEventArgs e)
		{
			base.OnDrawSubItem(e);
			e.DrawDefault = true;
		}
		protected override void OnColumnWidthChanging(ColumnWidthChangingEventArgs e)
		{
			base.OnColumnWidthChanging(e);
			if (e.ColumnIndex == 0 || e.ColumnIndex == 1)
			{
				e.NewWidth = Columns[e.ColumnIndex].Width;
				e.Cancel = true;
			}
		}

		private void OnNewMessageSet(ImmutableArray<ProjectMessage> messages)
		{
			Items.Clear();
			Items.AddRange(messages
				.OrderBy(x => x.OriginalMessage.Span.Start)
				.ThenBy(x => x.OriginalMessage.Span.Length)
				.Select(MessageToItem)
				.ToArray());
			Items.Add(GetFinalItem(messages));
		}
		private static ListViewItem GetFinalItem(ImmutableArray<ProjectMessage> messages)
		{
			var item = new ListViewItem();
			item.SubItems.Add($"{messages.Count(msg => msg.OriginalMessage.Critical)} errors, {messages.Count(msg => !msg.OriginalMessage.Critical)} warnings");
			item.Text = null;
			item.Tag = null;
			item.ImageKey = null;
			return item;
		}

		private static ListViewItem MessageToItem(ProjectMessage msg)
		{
			var item = new ListViewItem();
			item.SubItems.Add(msg.OriginalMessage.Text);
			item.Text = null;
			item.Tag = msg;
			item.ImageKey = (msg.OriginalMessage.Critical ? MessageType.Error : MessageType.Warning).ToString();
			return item;
		}

		public ListViewItem? FirstSelectedItem => SelectedItems.Count > 0 ? SelectedItems[0] : null;

		private SyntaxEditorControl EditorView { get; }

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			/*
			if (e.KeyCode == Keys.C && e.Control && SelectedItems.Count > 0)
				CopyItemsToClipboard(this.SelectedItems());
			*/
		}

		protected override void OnMouseClick(MouseEventArgs e)
		{
			base.OnMouseClick(e);
			/*
			if (e.Button == MouseButtons.Right)
			{
				var menu = new MessageListViewContextMenu(this);
				menu.Show(this, e.Location);
			}
			*/
		}

		protected override void OnMouseDoubleClick(MouseEventArgs e)
		{
			var item = FirstSelectedItem;
			if (item == null)
				return;

			if (item.Tag is ProjectMessage msg)
			{
				//var fileId = msg.OriginalMessage.Span.Start.File;
				//if (fileId == null)
				//return;
				//var editor = EditorView.ShowEditor(fileId);
				//if (editor == null)
				//return;
				if (msg.TryGetPosition(EditorView.TextBuffer.Snapshot) is IntSpan pos)
				{
					var start = pos.Start;
					if (start < EditorView.TextBuffer.Snapshot.Length)
					{
						if(pos.Length != 0)
							EditorView.Selection.Range = (pos.Start, pos.End);
						else
							EditorView.Caret.Position = pos.Start;
						EditorView.Focus();
					}
				}
			}
		}
	}
}
