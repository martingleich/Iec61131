using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Compiler;
using SyntaxEditor;
using StandardLibraryExtensions;

namespace FullEditor
{
	public sealed class SyntaxHighlightTagger : ITagger<ClassifierTag>
	{
		public static readonly ClassifierTag Keyword = new("keyword");
		public static readonly ClassifierTag Type = new("type");
		public static readonly ClassifierTag Variable = new("variable");
		public static readonly ClassifierTag Scope = new("scope");
		public static readonly ClassifierTag Literal = new("literal");

		public readonly SingleFileCompilerScheduler CompileService;
		public SyntaxHighlightTagger(SingleFileCompilerScheduler compileService)
		{
			CompileService = compileService ?? throw new ArgumentNullException(nameof(compileService));
		}
		public IEnumerable<TaggedSpan> GetTags(TextSnapshot snap, IntSpan span)
		{
			var text = snap.GetText();
			var parsed = CompileService.GetParsedPou(snap);
			var nodes = new ISyntax[] { parsed.Interface, parsed.Body };
			return SyntaxTreeUtils.GetAllTokens(nodes).Select(TryTagToken).WhereNotNullStruct();
		}

		private TaggedSpan? TryTagToken(IToken token)
		{
			if (token.Generating != null)
			{
				if (KeywordTokens.Contains(token.Generating))
					return new TaggedSpan(token.SourceSpan.Start.Offset, token.SourceSpan.Length, Keyword);
				else if (token is IntegerLiteralToken or RealLiteralToken or TypedLiteralToken)
					return new TaggedSpan(token.SourceSpan.Start.Offset, token.SourceSpan.Length, Literal);
			}

			return null;
		}
		private static readonly HashSet<string> KeywordTokens = ScannerKeywordTable.AllKeywords.ToHashSet(StringComparer.InvariantCultureIgnoreCase);
	}
}
