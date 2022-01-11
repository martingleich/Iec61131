using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
using SyntaxEditor;

namespace FullEditor
{
	public sealed class SyntaxHighlightProvider : ITaggerProvider, ISyntaxEditor_FormattingClasses_Provider, ISyntaxEditor_Options_Provider
	{
		public static readonly Option<Color> OptionKeywordColor = Option_NetForms.Create("text-color-keyword", Color.FromArgb(86, 156, 214));
		public static readonly Option<Color> OptionTypeColor = Option_NetForms.Create("text-color-type", Color.FromArgb(78, 201, 176));
		public static readonly Option<Color> OptionScopeColor = Option_NetForms.Create("text-color-scope", Color.FromArgb(184, 215, 163));
		public static readonly Option<Color> OptionLiteralColor = Option_NetForms.Create("text-color-literal", Color.FromArgb(255, 125, 39));

		public readonly SingleFileCompilerScheduler SingleFileScheduler;

		public SyntaxHighlightProvider(SingleFileCompilerScheduler compileService)
		{
			SingleFileScheduler = compileService ?? throw new ArgumentNullException(nameof(compileService));
		}

		ImmutableDictionary<ClassifierTag, Option<Color>> ISyntaxEditor_FormattingClasses_Provider.ClassifierMap { get; } = new Dictionary<ClassifierTag, Option<Color>>()
		{
			[SyntaxHighlightTagger.Keyword] = OptionKeywordColor,
			[SyntaxHighlightTagger.Literal] = OptionLiteralColor
		}.ToImmutableDictionary();

		ImmutableArray<IOption> ISyntaxEditor_Options_Provider.Options { get; } = ImmutableArray.Create<IOption>(
			OptionKeywordColor,
			OptionTypeColor,
			OptionScopeColor,
			OptionLiteralColor);

		ITagger<ITag> ITaggerProvider.Create(ISyntaxEditor synEd) => new SyntaxHighlightTagger(SingleFileScheduler);
	}
}
