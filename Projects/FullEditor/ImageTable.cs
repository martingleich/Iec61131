using System;
using System.Drawing;

namespace FullEditor.Resources
{
	public static class Images
	{
		private static readonly Lazy<Image> _Error = new (()
			=> ImageHelper.ResizeImage(SystemIcons.Error.ToBitmap(), 16, 16));
		private static readonly Lazy<Image> _Warning = new (()
			=> ImageHelper.ResizeImage(SystemIcons.Warning.ToBitmap(), 16, 16));
		public static Image Error => _Error.Value;
		public static Image Warning => _Warning.Value;
	}
}
