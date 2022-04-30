using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Runtime
{
	using IR;

	public sealed partial class Runtime
	{
		public sealed class PanicException : Exception
		{
			public readonly int Position;

			public PanicException(string message, int position) : base(message)
			{
				Position = position;
			}
		}

		private int _instructionCursor;
		private readonly List<CallFrame> _callStack = new();
		private CallFrame CurrentFrame => _callStack[^1];
		private readonly HashSet<int> _temporaryBreakpoints = new();
		private readonly HashSet<int> _breakpoints = new();

		private readonly ImmutableArray<byte[]> _memory;
		private readonly ImmutableDictionary<PouId, CompiledPou> _code;
		private readonly PouId _entryPoint;

		private record struct CallFrame(
			CompiledPou Compiled,
			ImmutableArray<LocalVarOffset> OutputsToCopy,
			int CallAddress,
			ushort Base)
		{
			public ImmutableArray<IStatement> Code => Compiled.Code;
			public readonly int StackSize => Compiled.StackUsage;

			public MemoryLocation this[LocalVarOffset offset] => new (STACK_AREA, (ushort)(Base + offset.Offset));
		}

		private const int STACK_AREA = 1;
		public Runtime(ImmutableArray<int> areaSizes, ImmutableDictionary<PouId, CompiledPou> code, PouId entryPoint)
		{
			_memory = areaSizes.Select(size => new byte[size]).ToImmutableArray();
			_code = code;
			_entryPoint = entryPoint;
		}

		public MemoryLocation LoadEffectiveAddress(int frameId, LocalVarOffset offset) => _callStack[frameId][offset];
		public MemoryLocation LoadEffectiveAddress(LocalVarOffset offset) => LoadEffectiveAddress(_callStack.Count - 1, offset);

        #region MemoryAccess
        public void Copy(MemoryLocation from, MemoryLocation to, int size)
		{
			Array.Copy(_memory[from.Area], from.Offset, _memory[to.Area], to.Offset, size);
		}
		public void WriteBits(ulong bits, MemoryLocation to, int size)
		{
			switch (size)
			{
				case 0:
					break;
				case 1:
					WriteSINT(to, unchecked((sbyte)bits));
					break;
				case 2:
					WriteINT(to, unchecked((short)bits));
					break;
				case 4:
					WriteDINT(to, unchecked((int)bits));
					break;
				case 8:
					WriteLINT(to, unchecked((long)bits));
					break;
				default: throw new ArgumentException($"{nameof(size)}({size}) must be either 1,2,4 or 8.");
			}
		}
        public ulong LoadBits(MemoryLocation from, int size) => size switch
        {
            1 => unchecked((ulong)LoadSINT(from)),
            2 => unchecked((ulong)LoadINT(from)),
            4 => unchecked((ulong)LoadDINT(from)),
            8 => unchecked((ulong)LoadLINT(from)),
            _ => throw new ArgumentException($"{nameof(size)}({size}) must be either 1,2,4 or 8."),
        };

        public MemoryLocation LoadPointer(LocalVarOffset offset)
		{
			var effective = LoadEffectiveAddress(offset);
			var area = _memory[effective.Area];
			ushort b0 = (ushort)(area[effective.Offset + 0] << 8);
			ushort b1 = area[effective.Offset + 1];
			ushort b2 = (ushort)(area[effective.Offset + 2] << 8);
			ushort b3 = area[effective.Offset + 3];
			return new MemoryLocation((ushort)(b0 | b1), (ushort)(b2 | b3));
		}
		
		public sbyte LoadSINT(MemoryLocation location)
		{
			var area = _memory[location.Area];
			return unchecked((sbyte)area[location.Offset]);
		}
		public sbyte LoadSINT(LocalVarOffset offset)
		{
			var effective = LoadEffectiveAddress(offset);
			return LoadSINT(effective);
		}
		public void WriteSINT(LocalVarOffset offset, sbyte value)
		{
			var effective = LoadEffectiveAddress(offset);
			WriteSINT(effective, value);
		}
		private void WriteSINT(MemoryLocation effective, sbyte value)
		{
			var area = _memory[effective.Area];
            area[effective.Offset + 0] = (byte)(value & 0xFF);
		}
		
		public short LoadINT(LocalVarOffset offset)
		{
			var effective = LoadEffectiveAddress(offset);
			return LoadINT(effective);
		}
		public short LoadINT(MemoryLocation effective)
		{
			var area = _memory[effective.Area];
			return (short)(area[effective.Offset] << 8 | area[effective.Offset + 1]);
		}
		public void WriteINT(LocalVarOffset offset, short value)
		{
			var effective = LoadEffectiveAddress(offset);
			WriteINT(effective, value);
		}
		public void WriteINT(MemoryLocation effective, short value)
		{
			var area = _memory[effective.Area];
            area[effective.Offset + 0] = (byte)((value >> 8) & 0xFF);
            area[effective.Offset + 1] = (byte)((value >> 0) & 0xFF);
		}
		public int LoadDINT(LocalVarOffset offset)
		{
			var effective = LoadEffectiveAddress(offset);
			return LoadDINT(effective);
		}
		public int LoadDINT(MemoryLocation effective)
		{
			var area = _memory[effective.Area];
			int result = 0;
			for (int i = 0; i < 4; ++i)
				result |= ((int)area[effective.Offset + i]) << (8 * (3 - i));
			return result;
		}
		public void WriteDINT(LocalVarOffset offset, int value)
		{
			var effective = LoadEffectiveAddress(offset);
			WriteDINT(effective, value);
		}
		public void WriteDINT(MemoryLocation effective, int bits)
		{
			var area = _memory[effective.Area];
            area[effective.Offset + 0] = (byte)((bits >> 24) & 0xFF);
            area[effective.Offset + 1] = (byte)((bits >> 16) & 0xFF);
            area[effective.Offset + 2] = (byte)((bits >> 8) & 0xFF);
            area[effective.Offset + 3] = (byte)((bits >> 0) & 0xFF);
		}
		public void WriteLINT(LocalVarOffset offset, long value)
		{
			var effective = LoadEffectiveAddress(offset);
			WriteLINT(effective, value);
		}
		public void WriteLINT(MemoryLocation effective, long value)
		{
			var area = _memory[effective.Area];
            area[effective.Offset + 0] = (byte)((value >> 56) & 0xFF);
            area[effective.Offset + 1] = (byte)((value >> 48) & 0xFF);
            area[effective.Offset + 2] = (byte)((value >> 40) & 0xFF);
            area[effective.Offset + 3] = (byte)((value >> 32) & 0xFF);
            area[effective.Offset + 4] = (byte)((value >> 24) & 0xFF);
            area[effective.Offset + 5] = (byte)((value >> 16) & 0xFF);
            area[effective.Offset + 6] = (byte)((value >> 8) & 0xFF);
            area[effective.Offset + 7] = (byte)((value >> 0) & 0xFF);
		}
		public long LoadLINT(LocalVarOffset offset)
		{
			var effective = LoadEffectiveAddress(offset);
			return LoadLINT(effective);
		}
		public long LoadLINT(MemoryLocation effective)
		{
			var area = _memory[effective.Area];
			long result = 0;
			for (int i = 0; i < 8; ++i)
				result |= ((long)area[effective.Offset + i]) << (8 * (7 - i));
			return result;
		}
		
		public void WriteREAL(LocalVarOffset offset, float value)
		{
			var intValue = BitConverter.SingleToInt32Bits(value);
			WriteDINT(offset, intValue);
		}
		public float LoadREAL(LocalVarOffset offset)
		{
			var effective = LoadEffectiveAddress(offset);
			return LoadREAL(effective);
		}
		public float LoadREAL(MemoryLocation effective)
		{
			var intValue = LoadDINT(effective);
			return BitConverter.Int32BitsToSingle(intValue);
		}
		public void WriteLREAL(LocalVarOffset offset, double value)
		{
			var lintValue = BitConverter.DoubleToInt64Bits(value);
			WriteLINT(offset, lintValue);
		}
		public double LoadLREAL(LocalVarOffset offset)
		{
			var effective = LoadEffectiveAddress(offset);
			return LoadLREAL(effective);
		}
		public double LoadLREAL(MemoryLocation effective)
		{
			var lintValue = LoadLINT(effective);
			return BitConverter.Int64BitsToDouble(lintValue);
		}
		public bool LoadBOOL(LocalVarOffset offset)
		{
			var effective = LoadEffectiveAddress(offset);
			return LoadBOOL(effective);
		}
		public bool LoadBOOL(MemoryLocation effective)
		{
			var value = LoadSINT(effective);
			if (value == 0)
				return false;
			else if (value == -1)
				return true;
			else
				throw Panic($"Invalid boolean value at {effective}: {value:2X}.");
		}
		public void WriteBOOL(LocalVarOffset offset, bool value)
		{
			WriteSINT(offset, value ? (sbyte)-1 : (sbyte)0);
		}
		public ImmutableArray<byte> ReadMemory(int area, int start, int length)
            => ImmutableArray.Create(_memory[area], start, length);
        #endregion

        #region Calls
        public int? Call(
			PouId callee,
			ImmutableArray<LocalVarOffset> inputs,
			ImmutableArray<LocalVarOffset> outputs)
		{
			if (TryBuiltInCall(callee, inputs, outputs))
				return null;

			return RealCall(callee, inputs, outputs);

            int RealCall(PouId callee, ImmutableArray<LocalVarOffset> inputs, ImmutableArray<LocalVarOffset> outputs)
            {
                var compiled = _code[callee];
				var newBase = (ushort)(CurrentFrame.Base + CurrentFrame.StackSize);
                _callStack.Add(new CallFrame(compiled, outputs, _instructionCursor, newBase));
                for (int i = 0; i < inputs.Length; ++i)
                {
                    var (argOffset, argType) = compiled.InputArgs[i];
                    var from = LoadEffectiveAddress(_callStack.Count - 2, inputs[i]);
                    var to = LoadEffectiveAddress(_callStack.Count - 1, argOffset);
                    Copy(from, to, argType.Size);
                }
                return 0;
            }
		}

		private CallFrame PopFrame()
        {
            var frame = _callStack[^1];
			_callStack.RemoveAt(_callStack.Count - 1);
			return frame;
        }
		public int Return()
		{
			var callFrame = PopFrame();
            for (int i = 0; i < callFrame.OutputsToCopy.Length; ++i)
            {
                var (argOffset, argType) = callFrame.Compiled.OutputArgs[i];
                var from = callFrame[argOffset];
                var to = LoadEffectiveAddress(callFrame.OutputsToCopy[i]);
                Copy(from, to, argType.Size);
            }
			return callFrame.CallAddress + 1;
		}
		public ImmutableArray<StackFrame> GetStackTrace()
		{
			var builder = ImmutableArray.CreateBuilder<StackFrame>(_callStack.Count);
			var curInstr = _instructionCursor;
            for(int i = _callStack.Count - 1; i >= 0; --i)
			{
				var frame = _callStack[i];
				builder.Add(new StackFrame(frame.Compiled, curInstr, i));
				curInstr = frame.CallAddress;
			}
			builder.Reverse();
			return builder.MoveToImmutable();
		}
        #endregion

        public PanicException Panic(string message) => new(message, _instructionCursor);

		public enum State
		{
			Running,
			EndOfProgram,
			Breakpoint,
		}
		public State Step(bool ignoreBreak=false)
		{
			if (!ignoreBreak && (_temporaryBreakpoints.Contains(_instructionCursor) || _breakpoints.Contains(_instructionCursor)))
			{
				_temporaryBreakpoints.Clear();
				return State.Breakpoint;
			}
			if (CurrentFrame.Code[_instructionCursor].Execute(this) is int nextInstruction)
				_instructionCursor = nextInstruction;
			else
				++_instructionCursor;
			if (_callStack.Count == 0)
				return State.EndOfProgram;
			return State.Running;
		}
		public void Reset()
		{
			foreach (var area in _memory)
				Array.Fill<byte>(area, 0);
			_callStack.Add(new CallFrame(_code[_entryPoint], ImmutableArray<LocalVarOffset>.Empty, 0, 0));
		}
		public void RunOnce()
		{
			Reset();
			while (Step() != State.EndOfProgram)
				;
		}
		public IEnumerable<int> TemporaryBreakpoints => _temporaryBreakpoints;
		public void SetTemporaryBreakpoints(ImmutableArray<int> newTemporaryBreakpoints)
		{
			_temporaryBreakpoints.Clear();
			foreach (var i in newTemporaryBreakpoints)
                _temporaryBreakpoints.Add(i);
		}
		public IEnumerable<int> Breakpoints => _breakpoints;
		public void SetBreakpoints(IEnumerable<int> newBreakpoints)
		{
			_breakpoints.Clear();
			foreach (var i in newBreakpoints)
                _breakpoints.Add(i);
		}
	}
    public record struct StackFrame(CompiledPou Cpou, int CurAddress, int FrameId) { }
}
