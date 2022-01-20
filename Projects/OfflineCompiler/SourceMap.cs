using System;
using Compiler;
using System.Collections.Immutable;
using System.Collections.Generic;

namespace OfflineCompiler
{
	public sealed class SourceMap
	{
		public sealed class SingleFile
		{
			public readonly string SourceFile;
			private readonly ImmutableArray<int> LineStarts;

			public SingleFile(string sourceFile, ImmutableArray<int> lineStarts)
			{
				SourceFile = sourceFile ?? throw new ArgumentNullException(nameof(sourceFile));
				LineStarts = lineStarts;
			}

			public static SingleFile Create(string sourceFile, string fileContent)
			{
				var offsets = ImmutableArray.CreateBuilder<int>();
				int i;
				offsets.Add(0);
				for (i = 0; i < fileContent.Length; ++i)
				{
					if (fileContent[i] == '\r' && i < fileContent.Length - 1 && fileContent[i + 1] == '\n') // Windows line ending
					{
						++i;
						offsets.Add(i + 1);
					}
					else if (fileContent[i] == '\n') // Linux line ending
					{
						offsets.Add(i + 1);
					}
				}
				return new(sourceFile, offsets.ToImmutable());

			}

			private (int, int) GetLineCollumn(int offset)
			{
				if (offset < 0)
					return (-1, -1);
				int pos = LineStarts.BinarySearch(offset);
				if (pos > 0)
					return (pos + 1, 0);
				int line = ~pos - 1;
				int lineStart = LineStarts[line];
				int collumn = offset - lineStart;
				return (line + 1, collumn + 1);

			}
			public string GetNameOf(int startOffset, int endOffset)
			{
				var (startLine, startCollumn) = GetLineCollumn(startOffset);
				var (endLine, endCollumn) = GetLineCollumn(endOffset);
				return $"{SourceFile}:{startLine}:{startCollumn}:{endLine}:{endCollumn}";
			}
		}

		private readonly Dictionary<string, SingleFile> Maps = new();
		public void AddFile(SingleFile file)
		{
			Maps.Add(file.SourceFile, file);
		}
		public string GetNameOf(SourceSpan span)
		{
			if (Maps.TryGetValue(span.Start.File, out var file))
				return file.GetNameOf(span.Start.Offset, span.End.Offset);
			else
				return span.Start.File;
		}
	}
}
