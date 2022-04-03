using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Runtime
{
	using IR;
	using System.Collections.Concurrent;

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
		private readonly Stack<CallFrame> _callStack = new();
		private CallFrame CurrentFrame => _callStack.Peek();

		private readonly ImmutableArray<byte[]> _memory;
		private readonly ImmutableDictionary<PouId, CompiledPou> _code;
		private readonly PouId _entryPoint;

		private readonly struct CallFrame
		{
			public readonly CompiledPou Compiled;
			public readonly ImmutableArray<LocalVarOffset> OutputsToCopy;
			public readonly int ReturnAddress;
			public readonly ushort Base;

			public ImmutableArray<IStatement> Code => Compiled.Code;
			public readonly int StackSize => Compiled.StackUsage;

			public CallFrame(ushort @base, int returnAddress, ImmutableArray<LocalVarOffset> outputsToCopy, CompiledPou compiled)
			{
				Base = @base;
				ReturnAddress = returnAddress;
				OutputsToCopy = outputsToCopy;
				Compiled = compiled;
			}
		}

		private const int STACK_AREA = 1;
		public Runtime(int[] areas, ImmutableDictionary<PouId, CompiledPou> code, PouId entryPoint)
		{
			_memory = areas.Select(size => new byte[size]).ToImmutableArray();
			_code = code;
			_entryPoint = entryPoint;
		}

		public MemoryLocation LoadEffectiveAddress(LocalVarOffset offset) => new(STACK_AREA, (ushort)(CurrentFrame.Base + offset.Offset));
		public void Copy(MemoryLocation from, MemoryLocation to, int size)
		{
			Array.Copy(_memory[from.Area], from.Offset, _memory[to.Area], to.Offset, size);
		}
		public void Copy(ulong bits, MemoryLocation to, int size)
		{
			var area = _memory[to.Area];
			switch (size)
			{
				case 0:
				case 1:
					area[0] = (byte)(bits & 0xFF);
					break;
				case 2:
					area[0] = (byte)((bits >> 8) & 0xFF);
					area[1] = (byte)((bits >> 0) & 0xFF);
					break;
				case 4:
					area[0] = (byte)((bits >> 24) & 0xFF);
					area[1] = (byte)((bits >> 16) & 0xFF);
					area[2] = (byte)((bits >> 8) & 0xFF);
					area[3] = (byte)((bits >> 0) & 0xFF);
					break;
				case 8:
					area[0] = (byte)((bits >> 56) & 0xFF);
					area[1] = (byte)((bits >> 48) & 0xFF);
					area[2] = (byte)((bits >> 40) & 0xFF);
					area[3] = (byte)((bits >> 32) & 0xFF);
					area[4] = (byte)((bits >> 24) & 0xFF);
					area[5] = (byte)((bits >> 16) & 0xFF);
					area[6] = (byte)((bits >> 8) & 0xFF);
					area[7] = (byte)((bits >> 0) & 0xFF);
					break;
				default: throw new InvalidOperationException();
			}
		}

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
			return unchecked((sbyte)(area[location.Offset]));
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
			area[effective.Offset] = unchecked((byte)value);
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
			for (int i = 0; i < 2; ++i)
				area[effective.Offset + i] = (byte)((value >> (8 * (1 - i))) & 0xFF);
		}
		public int LoadDINT(LocalVarOffset offset)
		{
			var effective = LoadEffectiveAddress(offset);
			return LoadDINT(effective);
		}

		private int LoadDINT(MemoryLocation effective)
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

		private void WriteDINT(MemoryLocation effective, int value)
		{
			var area = _memory[effective.Area];
			for (int i = 0; i < 4; ++i)
				area[effective.Offset + i] = (byte)((value >> (8 * (3 - i))) & 0xFF);
		}

		public void WriteLINT(LocalVarOffset offset, long value)
		{
			var effective = LoadEffectiveAddress(offset);
			var area = _memory[effective.Area];
			for (int i = 0; i < 8; ++i)
				area[effective.Offset + i] = (byte)((value >> (8 * (7 - i))) & 0xFF);
		}
		public long LoadLINT(LocalVarOffset offset)
		{
			var effective = LoadEffectiveAddress(offset);
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
			var intValue = LoadDINT(offset);
			return BitConverter.Int32BitsToSingle(intValue);
		}
		public void WriteLREAL(LocalVarOffset offset, double value)
		{
			var lintValue = BitConverter.DoubleToInt64Bits(value);
			WriteLINT(offset, lintValue);
		}
		public double LoadLREAL(LocalVarOffset offset)
		{
			var lintValue = LoadLINT(offset);
			return BitConverter.Int64BitsToDouble(lintValue);
		}
		public bool LoadBOOL(LocalVarOffset offset)
		{
			var effective = LoadEffectiveAddress(offset);
			var area = _memory[effective.Area];
			if (area[effective.Offset] == 0)
				return false;
			else if (area[effective.Offset] == 0xFF)
				return true;
			else
				throw Panic($"Invalid boolean value at {offset}: {area[effective.Offset]}.");
		}
		public void WriteBOOL(LocalVarOffset offset, bool value)
		{
			var effective = LoadEffectiveAddress(offset);
			var area = _memory[effective.Area];
			area[offset.Offset] = value ? (byte)0xFF : (byte)0;
		}

		public int? Call(
			PouId callee,
			ImmutableArray<LocalVarOffset> inputs,
			ImmutableArray<LocalVarOffset> outputs)
		{
			if (TryBuiltInCall(callee, inputs, outputs))
				return null;

			return RealCall(callee, inputs, outputs);
		}

		private int RealCall(PouId callee, ImmutableArray<LocalVarOffset> inputs, ImmutableArray<LocalVarOffset> outputs)
		{
			var compiled = _code[callee];
			var newBase = (ushort)(CurrentFrame.Base + CurrentFrame.StackSize);
			for (int i = 0; i < inputs.Length; ++i)
			{
				var (argOffset, argSize) = compiled.InputArgs[i];
				var from = LoadEffectiveAddress(inputs[i]);
				var to = new MemoryLocation(STACK_AREA, (ushort)(newBase + argOffset.Offset));
				Copy(from, to, argSize);
			}
			_callStack.Push(new CallFrame(newBase, _instructionCursor, outputs, compiled));
			return 0;
		}

		public int Return()
		{
			var callFrame = _callStack.Pop();
			if (callFrame.OutputsToCopy.Length > 0)
			{
				for (int i = 0; i < callFrame.OutputsToCopy.Length; ++i)
				{
					var (argOffset, argSize) = callFrame.Compiled.OutputArgs[i];
					var from = new MemoryLocation(STACK_AREA, (ushort)(callFrame.Base + callFrame.OutputsToCopy[i].Offset));
					var to = LoadEffectiveAddress(argOffset);
					Copy(from, to, argSize);
				}
			}
			return callFrame.ReturnAddress;
		}

		public PanicException Panic(string message) => new(message, _instructionCursor);

		public bool Step()
		{
			if (CurrentFrame.Code[_instructionCursor].Execute(this) is int nextInstruction)
				_instructionCursor = nextInstruction;
			else
				++_instructionCursor;
			return _callStack.Count == 0;
		}
		public void Reset()
		{
			_callStack.Push(new CallFrame(0, 0, ImmutableArray<LocalVarOffset>.Empty, _code[_entryPoint]));
		}
		public void RunOnce()
		{
			Reset();
			while (Step())
				;
		}
	}
}
