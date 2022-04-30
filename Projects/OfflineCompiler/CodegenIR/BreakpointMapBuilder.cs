using Compiler;
using Runtime.IR;
using System.Collections.Generic;
using System.Collections.Immutable;
using Range = Runtime.IR.Range;

namespace OfflineCompiler
{
	public readonly record struct BreakpointId(int Id) { }
	public sealed class BreakpointMapBuilder
	{
		private record BreakpointData(SourceSpan SourceSpan, Range<int> Instructions, List<int> Successors) { }
		private readonly List<BreakpointData> _breakpoints = new();
		public BreakpointId AddBreakpoint(
			SourceSpan sourceSpan,
			Range<int> instructions)
        {
			var breakpointData = new BreakpointData(sourceSpan, instructions, new());
			var newId = new BreakpointId(_breakpoints.Count);
			_breakpoints.Add(breakpointData);
			return newId;
		}
		public void SetPredecessor(BreakpointId breakpoint, BreakpointId predecessor) => SetSuccessor(predecessor, breakpoint);
		public void SetSuccessor(BreakpointId breakpoint, BreakpointId successor)
		{
			_breakpoints[breakpoint.Id].Successors.Add(successor.Id);
		}

		public BreakpointMap ToBreakpointMap(SourceMap.SingleFile sourceMap)
		{
			var sourceRanges = ImmutableArray.CreateBuilder<KeyValuePair<Range<SourceLC>, int>>();
			var instructionRanges = ImmutableArray.CreateBuilder<KeyValuePair<Range<int>, int>>();
			var index = 0;
			foreach (var (sourceSpan, instructions, breakpointSuccessors) in _breakpoints)
			{
				var txtStart = sourceMap.GetLineCollumn(sourceSpan.Start.Offset).GetValueOrDefault();
				var txtEnd = sourceMap.GetLineCollumn(sourceSpan.End.Offset).GetValueOrDefault();
				sourceRanges.Add(KeyValuePair.Create(Range.Create(txtStart, txtEnd), index));
				instructionRanges.Add(KeyValuePair.Create(instructions, index));
				++index;
			}
			sourceRanges.Sort((a, b) => a.Key.Start.CompareTo(b.Key.Start));
			instructionRanges.Sort((a, b) => a.Key.Start.CompareTo(b.Key.Start));

			var successorRanges = ImmutableArray.CreateBuilder<Range<int>>();
			var successors = ImmutableArray.CreateBuilder<int>();
			foreach (var breakpoint in _breakpoints)
			{
				int start = successors.Count;
				successors.AddRange(breakpoint.Successors);
				successorRanges.Add(Range.Create(start, successors.Count));
			}
			return new BreakpointMap(
				sourceRanges.ToImmutable(),
				instructionRanges.ToImmutable(),
				successorRanges.ToImmutable(),
				successors.ToImmutable());
		}
	}
}
