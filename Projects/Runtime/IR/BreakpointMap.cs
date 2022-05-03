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
					for (int i = _map._breakpointData[Id].Successors.Start; i < _map._breakpointData[Id].Successors.End; ++i)
						yield return new Breakpoint(_map, _map._successors[i]);
				}
			}
			public IEnumerable<Breakpoint> NextLineBreakpoints
			{
				get
				{
					HashSet<Breakpoint> checkedBreakpoints = new();
					Stack<Breakpoint> toVisit = new();
					toVisit.Push(this);
					List<Breakpoint> nextLineBreakpoints = new();
					while (toVisit.TryPop(out var popped))
					{
						if (checkedBreakpoints.Add(popped))
						{
							foreach (var succ in popped.Successors)
							{
								if (succ.ContainsLine(Txt.Start.Line))
									toVisit.Push(succ);
								else
									nextLineBreakpoints.Add(succ);
							}
						}
					}
					return nextLineBreakpoints;
				}
			}

			public Range<SourceLC> Txt => _map._sourceIndex[_map._breakpointData[Id].Source].Key;
			public Range<int> Instruction => _map._instructionIndex[_map._breakpointData[Id].Instructions].Key;

            public bool ContainsLine(int line) => Txt.Start.Line <= line && line <= Txt.End.Line;

            public override bool Equals(object? obj) => throw new NotImplementedException();
			public bool Equals(Breakpoint? other) => other != null && other.Id == Id;
			public override int GetHashCode() => Id.GetHashCode();
        }

		private readonly ImmutableArray<KeyValuePair<Range<SourceLC>, int>> _sourceIndex;
		private readonly ImmutableArray<KeyValuePair<Range<int>, int>> _instructionIndex; // breakpointId -> instruction -> breakpointId
		private readonly ImmutableArray<(Range<int> Successors, int Instructions, int Source)> _breakpointData; // breakpointId -> SuccessorsIdRange
		private readonly ImmutableArray<int> _successors; // successorId -> breakpointId

		public BreakpointMap(
			ImmutableArray<KeyValuePair<Range<SourceLC>, int>> sourceTable,
			ImmutableArray<KeyValuePair<Range<int>, int>> instructionTable,
			ImmutableArray<(Range<int> Successors, int Instructions, int Source)> data,
			ImmutableArray<int> successors)
		{
			_sourceIndex = sourceTable;
			_instructionIndex = instructionTable;
			_breakpointData = data;
			_successors = successors;
		}

		public Breakpoint? TryGetBreakpointBySource(int line, int? collumn)
		{
			var sourcelc = new SourceLC(line, collumn ?? -1);
			return BinarySearchRanges(_sourceIndex, sourcelc) is int idx
				? new Breakpoint(this, idx)
				: null;
		}
		public Breakpoint? FindBreakpointByInstruction(int instruction)
			=> BinarySearchRanges(_instructionIndex, instruction) is int idx
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
				bw.Write(_sourceIndex.Length);
				foreach (var x in _sourceIndex)
				{
					Write(bw, x.Key.Start);
					Write(bw, x.Key.End);
					bw.Write(x.Value);
				}
				bw.Write(_instructionIndex.Length);
				foreach (var x in _instructionIndex)
				{
					bw.Write(x.Key.Start);
					bw.Write(x.Key.End);
					bw.Write(x.Value);
				}
				bw.Write(_breakpointData.Length);
				foreach (var x in _breakpointData)
				{
					bw.Write(x.Successors.Start);
					bw.Write(x.Successors.End);
					bw.Write(x.Instructions);
					bw.Write(x.Source);
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

				int dataLength = br.ReadInt32();
				var data = ImmutableArray.CreateBuilder<(Range<int>, int, int)>(dataLength);
				for (int i = 0; i < dataLength; ++i)
				{
					var start = br.ReadInt32();
					var end = br.ReadInt32();
					var instructions = br.ReadInt32();
					var source = br.ReadInt32();
					data.Add((Range.Create(start, end), instructions, source));
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
					data.MoveToImmutable(),
					successors.MoveToImmutable());
			}
		}
	}
}
