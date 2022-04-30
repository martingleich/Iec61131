using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace Runtime.IR
{
    public sealed class BreakpointMap
	{
		public sealed class Breakpoint : IEquatable<Breakpoint>
		{
			private readonly BreakpointMap _map;
			public readonly int Id;

			public Breakpoint(BreakpointMap map, int id)
			{
				_map = map ?? throw new ArgumentNullException(nameof(map));
				Id = id;
			}

			public IEnumerable<Breakpoint> Successors
			{
				get
				{
					for (int i = _map._successorsIds[Id].Start; i < _map._successorsIds[Id].End; ++i)
						yield return new Breakpoint(_map, _map._successors[i]);
				}
			}

			public Range<SourceLC> Txt => _map._sourceTable[Id].Key;
			public Range<int> Instruction => _map._instructionTable[Id].Key;

            public bool ContainsLine(int line) => Txt.Start.Line <= line && line <= Txt.End.Line;

            public override bool Equals(object? obj) => throw new NotImplementedException();
			public bool Equals(Breakpoint? other) => other != null && other.Id == Id;
			public override int GetHashCode() => Id.GetHashCode();
        }

		private readonly ImmutableArray<KeyValuePair<Range<SourceLC>, int>> _sourceTable;
		private readonly ImmutableArray<KeyValuePair<Range<int>, int>> _instructionTable;
		private readonly ImmutableArray<Range<int>> _successorsIds;
		private readonly ImmutableArray<int> _successors;

		public BreakpointMap(
			ImmutableArray<KeyValuePair<Range<SourceLC>, int>> sourceTable,
			ImmutableArray<KeyValuePair<Range<int>, int>> instructionTable,
			ImmutableArray<Range<int>> successorsIds,
			ImmutableArray<int> successors)
		{
			_sourceTable = sourceTable;
			_instructionTable = instructionTable;
			_successorsIds = successorsIds;
			_successors = successors;
		}

		public Breakpoint? TryGetBreakpointBySource(int line, int? collumn)
		{
			var sourcelc = new SourceLC(line, collumn ?? -1);
			return BinarySearchRanges(_sourceTable, sourcelc) is int idx
				? new Breakpoint(this, idx)
				: null;
		}
		public Breakpoint? FindBreakpointByInstruction(int instruction)
			=> BinarySearchRanges(_instructionTable, instruction) is int idx
				? new Breakpoint(this, idx)
				: null;
		private static int? BinarySearchRanges<T>(ImmutableArray<KeyValuePair<Range<T>, int>> ranges, T value) where T : IComparable<T>
		{
			var idx = ranges.BinarySearch(KeyValuePair.Create(Range.Create(value, value), 0), RangeKeyArrayComparer<T, int>.Instance);
			if (idx >= 0)
				return idx;

			int next = ~idx;
			int previous = next - 1;
			for (int i = Math.Max(0, previous); i <= Math.Min(next, ranges.Length - 1); ++i)
			{
                Range<T> range = ranges[i].Key;
                if (range.Start.CompareTo(value) <= 0 && (ranges.Length - 1 != i || value.CompareTo(range.End) < 0))
                    return ranges[i].Value;
            }

			return null;
		}
		
		public void SerializeToStream(Stream stream)
		{
			static void Write(BinaryWriter bw, SourceLC source)
			{
				bw.Write(source.Line);
				bw.Write(source.Collumn);
			}
			using (var bw = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
			{
				bw.Write(_sourceTable.Length);
				foreach (var x in _sourceTable)
				{
					Write(bw, x.Key.Start);
					Write(bw, x.Key.End);
					bw.Write(x.Value);
				}
				bw.Write(_instructionTable.Length);
				foreach (var x in _instructionTable)
				{
					bw.Write(x.Key.Start);
					bw.Write(x.Key.End);
					bw.Write(x.Value);
				}
				bw.Write(_successorsIds.Length);
				foreach (var x in _successorsIds)
				{
					bw.Write(x.Start);
					bw.Write(x.End);
				}
				bw.Write(_successors.Length);
				foreach (var x in _successors)
				{
					bw.Write(x);
				}
			}
		}

		public static BreakpointMap DeserializeFromStream(Stream stream)
		{
			static SourceLC ReadSourceLC(BinaryReader bw)
			{
				var line = bw.ReadInt32();
				var collum = bw.ReadInt32();
				return new SourceLC(line, collum);
			}
			using (var br = new BinaryReader(stream, System.Text.Encoding.UTF8, true))
			{
				var sourceTableLength = br.ReadInt32();
				var sourceTable = ImmutableArray.CreateBuilder<KeyValuePair<Range<SourceLC>, int>>(sourceTableLength);
				for (int i = 0; i < sourceTableLength; ++i)
				{
					var start = ReadSourceLC(br);
					var end = ReadSourceLC(br);
					var value = br.ReadInt32();
					sourceTable.Add(KeyValuePair.Create(Range.Create(start, end), value));
				}

				int instructionTableLength = br.ReadInt32();
				var instructionTable = ImmutableArray.CreateBuilder<KeyValuePair<Range<int>, int>>(instructionTableLength);
				for (int i = 0; i < instructionTableLength; ++i)
				{
					var start = br.ReadInt32();
					var end = br.ReadInt32();
					var value = br.ReadInt32();
					instructionTable.Add(KeyValuePair.Create(Range.Create(start, end), value));
				}

				int successorIdsLength = br.ReadInt32();
				var successorIds = ImmutableArray.CreateBuilder<Range<int>>(successorIdsLength);
				for (int i = 0; i < successorIdsLength; ++i)
				{
					var start = br.ReadInt32();
					var end = br.ReadInt32();
					successorIds.Add(Range.Create(start, end));
				}

				int successorsLength = br.ReadInt32();
				var successors = ImmutableArray.CreateBuilder<int>(successorsLength);
				for (int i = 0; i < successorsLength; ++i)
				{
					var value = br.ReadInt32();
					successors.Add(value);
				}

				return new BreakpointMap(
					sourceTable.MoveToImmutable(),
					instructionTable.MoveToImmutable(),
					successorIds.MoveToImmutable(),
					successors.MoveToImmutable());
			}
		}
	}
}
