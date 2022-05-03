using Xunit;

namespace RuntimeTests
{
    using Runtime.IR;
    using Runtime.IR.Statements;
    using Runtime.IR.Expressions;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using Runtime = Runtime.Runtime;
    using System;
    using System.Linq;
    using Type = Runtime.IR.Type;

    public class AssemblyTests
    {
        private static Runtime MakeRt(int memSize, params CompiledPou[] pous)
        {
            var rt = new Runtime(
                ImmutableArray.Create(0, memSize),
                pous.ToImmutableDictionary(k => k.Id),
                pous[0].Id);
            rt.Reset();
            return rt;
        }
        private static readonly LocalVarOffset Off0 = new(0);
        private static readonly LocalVarOffset Off1 = new(1);
        private static readonly LocalVarOffset Off2 = new(2);
        private static readonly LocalVarOffset Off4 = new(4);
        private static readonly LocalVarOffset Off6 = new(6);
        private static readonly LocalVarOffset Off8 = new(8);
        public static readonly Action<ImmutableArray<byte>> IsZero = bytes => bytes.All(b => b == 0);
        private static long LoadXInt(Runtime rt, LocalVarOffset offset, Type type) => unchecked((long)rt.LoadBits(rt.LoadEffectiveAddress(offset), type.Size));
        private static void AssertRunSteps(Runtime rt, int count)
        {
            for (int i = 0; i < count; i++)
                Assert.Equal(Runtime.State.Running, rt.Step());
        }
        [Fact]
        public void ReturnOfMain()
        {
            var rt = MakeRt(16, pous:
                CompiledPou.Action(new PouId("Main"), 0,
                    Return.Instance));
            Assert.Equal(Runtime.State.EndOfProgram, rt.Step());
        }
        [Fact]
        public void IgnoreComment()
        {
            var rt = MakeRt(16, pous:
                CompiledPou.Action(new PouId("Main"), 0,
                    new Comment("This is a comment"),
                    Return.Instance));
            Assert.Equal(Runtime.State.Running, rt.Step());
            var mem = rt.ReadMemory(1, 0, 16);
            IsZero(mem);
            Assert.Equal(Runtime.State.EndOfProgram, rt.Step());
        }
        [Theory]
        [InlineData(123, 1)]
        [InlineData(12345, 2)]
        [InlineData(12345679, 4)]
        [InlineData(12345679101112, 8)]
        public void CopyXByteToStack(long arg, ushort size)
        {
            var type = new Type(size);
            var rt = MakeRt(32,
                CompiledPou.Action(new PouId("Main"), 0,
                    WriteValue.WriteLiteral(unchecked((ulong)arg), Off0, size),
                    WriteValue.WriteLocal(Off0, new LocalVarOffset(size), size),
                    Return.Instance));
            Assert.Equal(Runtime.State.Running, rt.Step());
            Assert.Equal(arg, LoadXInt(rt, Off0, type));
            Assert.Equal(Runtime.State.Running, rt.Step());
            Assert.Equal(arg, LoadXInt(rt, new LocalVarOffset(size), type));
            Assert.Equal(Runtime.State.EndOfProgram, rt.Step());
        }
        [Theory]
        [InlineData(123, 1)]
        [InlineData(12345, 2)]
        [InlineData(12345679, 4)]
        [InlineData(12345679101112, 8)]
        public void CopyXByteToPointer(long arg, ushort size)
        {
            var type = new Type(size);
            var rt = MakeRt(32,
                CompiledPou.Action(new PouId("Main"), 0,
                    new WriteValue(LiteralExpression.FromMemoryLocation(new MemoryLocation(1, size)), Off0, 4),
                    new WriteDerefValue(new LiteralExpression(unchecked((ulong)arg)), Off0, size),
                    Return.Instance));
            Assert.Equal(Runtime.State.Running, rt.Step());
            Assert.Equal(Runtime.State.Running, rt.Step());
            Assert.Equal(arg, LoadXInt(rt, new LocalVarOffset(size), type));
            Assert.Equal(Runtime.State.EndOfProgram, rt.Step());
        }

        [Fact]
        public void TemporaryBreakpoints()
        {
            var mainId = new PouId("Main");
            var rt = MakeRt(32,
                CompiledPou.Action(mainId, 0,
                    WriteValue.WriteLiteral(123, Off0, 2),
                    WriteValue.WriteLiteral(321, Off0, 2),
                    Return.Instance));
            Assert.Empty(rt.TemporaryBreakpoints); // Start out empty
            rt.SetTemporaryBreakpoints(ImmutableArray.Create((mainId, 55)));
            Assert.Equal(rt.TemporaryBreakpoints.ToHashSet(), new HashSet<(PouId, int)>() { (mainId, 55) });
            rt.SetTemporaryBreakpoints(ImmutableArray.Create((mainId, 0), (mainId, 1)));
            Assert.Equal(rt.TemporaryBreakpoints.ToHashSet(), new HashSet<(PouId, int)>() { (mainId, 0), (mainId, 1) }); // Old temp breakpoins are cleared
            Assert.Equal(Runtime.State.Breakpoint, rt.Step());
            Assert.Empty(rt.TemporaryBreakpoints);
            var frame = Assert.Single(rt.GetStackTrace());
            Assert.Equal(0, frame.CurAddress);
        }
        [Fact]
        public void TemporaryBreakpoints_KeepsNormalBreakpoint()
        {
            var mainId = new PouId("Main");
            var rt = MakeRt(32,
                CompiledPou.Action(mainId, 0,
                    WriteValue.WriteLiteral(123, Off0, 2),
                    WriteValue.WriteLiteral(321, Off0, 2),
                    Return.Instance));
            rt.SetTemporaryBreakpoints(ImmutableArray.Create((mainId, 0)));
            rt.SetBreakpoints(ImmutableArray.Create((mainId, 1)));
            Assert.Equal(rt.TemporaryBreakpoints.ToHashSet(), new HashSet<(PouId, int)>() { (mainId, 0) });
            Assert.Equal(rt.Breakpoints.ToHashSet(), new HashSet<(PouId, int)>() { (mainId, 1) });
            Assert.Equal(Runtime.State.Breakpoint, rt.Step());
            Assert.Empty(rt.TemporaryBreakpoints);
            Assert.Equal(rt.Breakpoints.ToHashSet(), new HashSet<(PouId, int)>() { (mainId, 1) });
            var frame = Assert.Single(rt.GetStackTrace());
            Assert.Equal(0, frame.CurAddress);
        }
        [Fact]
        public void NormalBreakpoins_RemovesTemporaryBreakpoints()
        {
            var mainId = new PouId("Main");
            var rt = MakeRt(32,
                CompiledPou.Action(mainId, 0,
                    WriteValue.WriteLiteral(123, Off0, 2),
                    WriteValue.WriteLiteral(321, Off0, 2),
                    Return.Instance));
            rt.SetBreakpoints(ImmutableArray.Create((mainId, 0)));
            rt.SetTemporaryBreakpoints(ImmutableArray.Create((mainId, 1)));
            Assert.Equal(rt.TemporaryBreakpoints.ToHashSet(), new HashSet<(PouId, int)>() { (mainId, 1) });
            Assert.Equal(rt.Breakpoints.ToHashSet(), new HashSet<(PouId, int)>() { (mainId, 0) });
            Assert.Equal(Runtime.State.Breakpoint, rt.Step());
            Assert.Empty(rt.TemporaryBreakpoints);
            Assert.Equal(rt.Breakpoints.ToHashSet(), new HashSet<(PouId, int)>() { (mainId, 0) });
            var frame = Assert.Single(rt.GetStackTrace());
            Assert.Equal(0, frame.CurAddress);
        }

        [Theory]
        [InlineData(67, 45, "SINT", 1, "ADD")]
        [InlineData(123, 456, "INT", 2, "ADD")]
        [InlineData(23542345, 4524355, "DINT", 4, "ADD")]
        [InlineData(235423454352, 452435556, "LINT", 8, "ADD")]

        [InlineData(67, 45, "SINT", 1, "SUB")]
        [InlineData(123, 456, "INT", 2, "SUB")]
        [InlineData(23542345, 4524355, "DINT", 4, "SUB")]
        [InlineData(235423454352, 452435556, "LINT", 8, "SUB")]

        [InlineData(3, 7, "SINT", 1, "MUL")]
        [InlineData(32, 17, "INT", 2, "MUL")]
        [InlineData(4652, 542, "DINT", 4, "MUL")]
        [InlineData(452, 6245, "LINT", 8, "MUL")]

        [InlineData(7, 3, "SINT", 1, "DIV")]
        [InlineData(32, 17, "INT", 2, "DIV")]
        [InlineData(465223, 542, "DINT", 4, "DIV")]
        [InlineData(4522315435, 6245, "LINT", 8, "DIV")]

        [InlineData(7, 3, "SINT", 1, "MOD")]
        [InlineData(32, 17, "INT", 2, "MOD")]
        [InlineData(465223, 542, "DINT", 4, "MOD")]
        [InlineData(4522315435, 6245, "LINT", 8, "MOD")]
        public void BuiltInCall_AddSubMulXInt(ulong v1, ulong v2, string type, int size, string op)
        {
            var rt = MakeRt(32,
                CompiledPou.Action(new PouId("Main"), size * 2,
                WriteValue.WriteLiteral(v1, Off0, size),
                WriteValue.WriteLiteral(v2, new LocalVarOffset((ushort)size), size),
                new StaticCall(new PouId($"__SYSTEM::{op}_{type}"),
                    ImmutableArray.Create(Off0, new LocalVarOffset((ushort)size)),
                    ImmutableArray.Create(Off0)),
                Return.Instance));
            AssertRunSteps(rt, 3);
            var value = LoadXInt(rt, Off0, new Type(size));
            var result = op switch
            {
                "ADD" => v1 + v2,
                "SUB" => v1 - v2,
                "MUL" => v1 * v2,
                "DIV" => v1 / v2,
                "MOD" => v1 % v2,
                _ => throw new InvalidOperationException()
            };
            Assert.Equal((long)result, value);
        }

        [Theory]
        [InlineData("SINT", 1)]
        [InlineData("INT", 2)]
        [InlineData("DINT", 4)]
        [InlineData("LINT", 8)]
        public void PanicDivByZero(string type, int size)
        {
            var rt = MakeRt(32,
                CompiledPou.Action(new PouId("Main"), size * 2,
                WriteValue.WriteLiteral(123, Off0, size),
                WriteValue.WriteLiteral(0, new LocalVarOffset((ushort)size), size),
                new StaticCall(new PouId($"__SYSTEM::DIV_{type}"),
                    ImmutableArray.Create(Off0, new LocalVarOffset((ushort)size)),
                    ImmutableArray.Create(Off0)),
                Return.Instance));
            rt.Step();
            rt.Step();
            var exp = Assert.Throws<Runtime.PanicException>(() => rt.Step());
            Assert.Equal(2, exp.Position);
        }

        [Fact]
        public void RealCall()
        {
            // MAIN:
            //     Func(in1, in2, out1, out2)
            // Func:
            //     out1 := in1 * 2,
            //     out2 := in2 + 2,
            var rt = MakeRt(32,
                CompiledPou.Action(new PouId("Main"), 4,
                    WriteValue.WriteLiteral(12, Off0, 2),
                    WriteValue.WriteLiteral(13, Off2, 2),
                    new StaticCall(new PouId("Func"),
                        ImmutableArray.Create(Off0, Off2),
                        ImmutableArray.Create(Off0, Off2)),
                    Return.Instance),
                new CompiledPou(new PouId("Func"), 2,
                    ImmutableArray.Create(
                        new CompiledArgument(new LocalVarOffset(0), Type.Bits16),
                        new CompiledArgument(new LocalVarOffset(2), Type.Bits16)),
                    ImmutableArray.Create(
                        new CompiledArgument(new LocalVarOffset(4), Type.Bits16),
                        new CompiledArgument(new LocalVarOffset(6), Type.Bits16)),
                    ImmutableArray.Create<IStatement>(
                            WriteValue.WriteLiteral(2, Off8, 2),
                            new StaticCall(new PouId("__SYSTEM::MUL_INT"),
                                ImmutableArray.Create(Off0, Off8),
                                ImmutableArray.Create(Off4)),
                            new StaticCall(new PouId("__SYSTEM::ADD_INT"),
                                ImmutableArray.Create(Off2, Off8),
                                ImmutableArray.Create(Off6)),
                            Return.Instance)));
            var stacktrace1 = rt.GetStackTrace();
            Assert.Collection(stacktrace1,
                frame => { Assert.Equal("Main", frame.Cpou.Id.Name); Assert.Equal(0, frame.FrameId); Assert.Equal(0, frame.CurAddress); });
            AssertRunSteps(rt, 3);
            var stacktrace2 = rt.GetStackTrace();
            Assert.Collection(stacktrace2,
                frame => { Assert.Equal("Main", frame.Cpou.Id.Name); Assert.Equal(0, frame.FrameId); Assert.Equal(2, frame.CurAddress); },
                frame => { Assert.Equal("Func", frame.Cpou.Id.Name); Assert.Equal(1, frame.FrameId); Assert.Equal(0, frame.CurAddress); });
            AssertRunSteps(rt, 4);
            var stacktrace3 = rt.GetStackTrace();
            Assert.Collection(stacktrace3,
                frame => { Assert.Equal("Main", frame.Cpou.Id.Name); Assert.Equal(0, frame.FrameId); Assert.Equal(3, frame.CurAddress); });
            var off0 = LoadXInt(rt, Off0, Type.Bits16);
            var off2 = LoadXInt(rt, Off2, Type.Bits16);
            Assert.Equal(24, off0);
            Assert.Equal(15, off2);
            Assert.Equal(Runtime.State.EndOfProgram, rt.Step());
            Assert.Empty(rt.GetStackTrace());
        }

        [Fact]
        public void Jump()
        {
            var label = new Label("MyLabel", 1);
            var rt = MakeRt(2,
                CompiledPou.Action(new PouId("Main"), 2,
                    WriteValue.WriteLiteral(1, Off0, 1),
                    label,
                    StaticCall.Func(new PouId("__SYSTEM::ADD_SINT"), Off1, Off0, Off1),
                    new Jump(label)));
            rt.Step();
            for (int i = 0; i < 10; ++i)
            {
                Assert.Equal(1 + i % 3, Assert.Single(rt.GetStackTrace()).CurAddress);
                Assert.Equal(Runtime.State.Running, rt.Step());
            }
        }
        [Fact]
        public void ConditionalJump_Taken()
        {
            var label = new Label("MyLabel", 0);
            var rt = MakeRt(2,
                CompiledPou.Action(new PouId("Main"), 2,
                    label,
                    WriteValue.WriteLiteral(0, Off0, 1),
                    new JumpIfNot(Off0, label)));
            for (int i = 0; i < 10; ++i)
            {
                Assert.Equal(i % 3, Assert.Single(rt.GetStackTrace()).CurAddress);
                Assert.Equal(Runtime.State.Running, rt.Step());
            }
        }
        [Fact]
        public void ConditionalJump_NonTaken()
        {
            var label = new Label("MyLabel", 0);
            var rt = MakeRt(2,
                CompiledPou.Action(new PouId("Main"), 2,
                    label,
                    WriteValue.WriteLiteral(0xFF, Off0, 1),
                    new JumpIfNot(Off0, label),
                    Return.Instance));
            AssertRunSteps(rt, 3);
            Assert.Equal(Runtime.State.EndOfProgram, rt.Step());
        }
    }
}
