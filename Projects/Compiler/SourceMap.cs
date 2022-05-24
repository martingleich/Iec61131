using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.IO;
using Compiler.Messages;
using Runtime.IR;

namespace Compiler
{
    public sealed class SourceMap
	{
		public sealed class SingleFile
		{
			public readonly string FullPath; // The full path of the file in the filesystem.
			public readonly string SourceFile; // The name of the file in the language source.
			private readonly ImmutableArray<int> LineStarts;

			public SingleFile(string fullPath, string sourceFile, ImmutableArray<int> lineStarts)
			{
				FullPath = fullPath ?? throw new ArgumentNullException(nameof(fullPath));
				SourceFile = sourceFile ?? throw new ArgumentNullException(nameof(sourceFile));
				LineStarts = lineStarts;
			}

			public static SingleFile Create(FileInfo fileInfo, string fileContent)
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
				return new(fileInfo.FullName, fileInfo.Name, offsets.ToImmutable());

			}

			public SourceLC? GetLineCollumn(int offset)
			{
				if (offset < 0)
					return null;
				int line, collumn;
				int pos = LineStarts.BinarySearch(offset);
				if (pos >= 0)
				{
					line = pos;
					collumn = 0;
				}
				else
				{
					line = ~pos - 1;
					collumn = offset - LineStarts[line];
				}
				return new SourceLC(line + 1, collumn + 1);

			}
			public string GetNameOf(int startOffset, int endOffset)
			{
				var start = GetLineCollumn(startOffset).GetValueOrDefault();
				var end= GetLineCollumn(endOffset).GetValueOrDefault();
				return $"{SourceFile}:{start.Line}:{start.Collumn}:{end.Line}:{end.Collumn}";
			}
		}

		public SingleFile? GetFile(string? sourceFile)
		{
			if (sourceFile == null)
				return null;
			return Maps[sourceFile];
		}

		private readonly ImmutableDictionary<string, SingleFile> Maps;
		public SourceMap(IEnumerable<SingleFile> files)
		{
			Maps = files.ToImmutableDictionary(x => x.SourceFile);
		}
		public string GetNameOf(SourceSpan span)
		{
			if (span.Start.File is null)
				return "";
			if (Maps.TryGetValue(span.Start.File, out var file))
				return file.GetNameOf(span.Start.Offset, span.End.Offset);
			else
				return span.Start.File;
		}

		public IMessageFormatter GetMessageFormatter() => new SourceMapMessageFormatter(this);
        private sealed class SourceMapMessageFormatter : IMessageFormatter
        {
            private readonly SourceMap _sourceMap;

            public SourceMapMessageFormatter(SourceMap sourceMap)
            {
                _sourceMap = sourceMap;
            }

            public string GetKindName(bool critical) => MessageFormatter.Null.GetKindName(critical);
            public string GetSourceName(SourceSpan span) => _sourceMap.GetNameOf(span);
        }
	}
}
