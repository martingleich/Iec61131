using System;
using Compiler;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.IO;

namespace OfflineCompiler
{
	public sealed class SourceMap
	{
		public sealed class SingleFile
		{
			public readonly string FullPath;
			public readonly string FileName;
			private readonly ImmutableArray<int> LineStarts;

			public SingleFile(string fullSourceFile, string sourceFile, ImmutableArray<int> lineStarts)
			{
				FullPath = fullSourceFile;
				FileName = sourceFile ?? throw new ArgumentNullException(nameof(sourceFile));
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

			public (int, int)? GetLineCollumn(int offset)
			{
				if (offset < 0)
					return null;
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
				var (startLine, startCollumn) = GetLineCollumn(startOffset) ?? (-1, -1);
				var (endLine, endCollumn) = GetLineCollumn(endOffset) ?? (-1, -1);
				return $"{FileName}:{startLine}:{startCollumn}:{endLine}:{endCollumn}";
			}
		}

		public SingleFile? GetFile(string? file)
		{
			if (file == null)
				return null;
			return Maps[file];
		}

		private readonly Dictionary<string, SingleFile> Maps = new();
		public void Add(SingleFile file)
		{
			Maps.Add(file.FileName, file);
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
		public (int, int)? GetLineCollumn(SourcePoint point)
		{
			if (point.File is string filePath && Maps.TryGetValue(filePath, out var file))
				return file.GetLineCollumn(point.Offset);
			else
				return null;
		}
	}
}
