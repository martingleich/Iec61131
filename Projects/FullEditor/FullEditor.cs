using System.Drawing;
using System.Windows.Forms;
using SyntaxEditor;
using System.Reactive.Linq;
using System;

namespace FullEditor
{
	public partial class FullEditor : Form
	{
		public FullEditor()
		{
			InitializeComponent();
			var scheduler = new SingleFileCompilerScheduler("MyFile");

			var synEd = new SyntaxEditorControl(
				SyntaxEditor.Layers.Background.Provider,
				SyntaxEditor.Layers.Caret.Provider,
				SyntaxEditor.Layers.Text.Provider,
				SyntaxEditor.Margins.VScrollBar.Provider,
				SyntaxEditor.Margins.LineNumbering.Provider,
				new SyntaxHighlightProvider(scheduler))
			{
				Font = new Font(FontFamily.GenericMonospace, 12),
				Dock = DockStyle.Fill
			};
			synEd.TextBuffer.OnNewSnapshot.Throttle(TimeSpan.FromSeconds(0.5)).Subscribe(scheduler.SetNewSnapshot);

			synEd.Options.GetOptionStorageByName<Color>("text-color").Value = Color.FromArgb(220, 220, 220);
			synEd.Options.GetOptionStorageByName<Color>("background-color").Value = Color.FromArgb(30, 30, 30);
			synEd.Options.GetOptionStorageByName<Color>("line-number-color").Value = Color.FromArgb(43, 145, 175);
			synEd.Options.GetOptionStorageByName<Color>("line-number-background-color").Value = Color.FromArgb(30, 30, 30);
			synEd.Options.GetOptionStorageByName<Color>("selected-color").Value = Color.FromArgb(51, 153, 225);
			synEd.Options.GetOptionStorageByName<Color>("highlight-line-color").Value = Color.FromArgb(70, 70, 70);
			synEd.Options.GetOptionStorageByName<bool>("highlight-line").Value = false;

			var messageView = new MessageView(scheduler.OnNewMessages, synEd);
			messageView.Dock = DockStyle.Fill;
			messageView.BackColor = Color.FromArgb(30, 30, 30);
			messageView.ForeColor = Color.FromArgb(220, 220, 220);

			var splitContainer = new SplitContainer();
			splitContainer.Dock = DockStyle.Fill;
			splitContainer.Orientation = Orientation.Horizontal;
			splitContainer.Panel1.Controls.Add(synEd);
			splitContainer.Panel2.Controls.Add(messageView);

			Controls.Add(splitContainer);
		}
	}
}
