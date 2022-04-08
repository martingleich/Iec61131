using Compiler;
using Runtime.IR;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using static Runtime.IR.BreakpointMap;
using Range = Runtime.IR.Range;

namespace OfflineCompiler
{
	public readonly struct BreakpointId
	{
		public readonly int Id;

		public BreakpointId(int id)
		{
			Id = id;
		}
	}

	public sealed class BreakpointFactory
	{
		private record BreakpointData(SourceSpan SourceSpan, int InstructionBegin, int InstructionEnd, List<int> Successors) { }
		private readonly List<BreakpointData> _breakpoints = new();
		public BreakpointId AddBreakpoint(
			SourceSpan sourceSpan,
			int instructionBegin,
			int instructionEnd)
		{
			var breakpointData = new BreakpointData(sourceSpan, instructionBegin, instructionEnd, new());
			var newId = new BreakpointId(_breakpoints.Count);
			_breakpoints.Add(breakpointData);
			return newId;
		}
		public BreakpointId AddBreakpoint(
			SourceSpan sourceSpan,
			int instructionBegin,
			int instructionEnd,
			BreakpointId? predecessorBreakpoint)
		{
			var breakpoint = AddBreakpoint(sourceSpan, instructionBegin, instructionEnd);
			if (predecessorBreakpoint is BreakpointId predecessor)
				SetSuccessor(predecessor, breakpoint);
			return breakpoint;
		}
		public void SetPredecessor(BreakpointId breakpoint, BreakpointId predecessor) => SetSuccessor(predecessor, breakpoint);
		public void SetSuccessor(BreakpointId breakpoint, BreakpointId successor)
		{
			_breakpoints[breakpoint.Id].Successors.Add(successor.Id);
		}

		public BreakpointMap ToBreakpointMap(SourceMap sourceMap)
		{
			var sourceRanges = ImmutableArray.CreateBuilder<KeyValuePair<Range<SourceLC>, int>>();
			var instructionRanges = ImmutableArray.CreateBuilder<KeyValuePair<Range<int>, int>>();
			var index = 0;
			foreach (var breakpoint in _breakpoints)
			{
				breakpoint.Deconstruct(out var sourceSpan, out var instructionBegin, out var instructionEnd, out var breakpointSuccessors);
				var startPos = sourceMap.GetLineCollumn(sourceSpan.Start);
				var endPos = sourceMap.GetLineCollumn(sourceSpan.End);
				if (!startPos.HasValue || !endPos.HasValue)
					throw new InvalidOperationException();
				var startPosLC = new SourceLC(startPos.Value.Item1, startPos.Value.Item2);
				var endPosLC = new SourceLC(endPos.Value.Item1, endPos.Value.Item2);
				sourceRanges.Add(KeyValuePair.Create(Range.Create(startPosLC, endPosLC), index));
				instructionRanges.Add(KeyValuePair.Create(Range.Create(instructionBegin, instructionEnd), index));
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
