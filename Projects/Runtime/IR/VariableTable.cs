using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Runtime.IR
{
    public sealed class VariableTable
	{
		public abstract record class StackVariable(string Name, LocalVarOffset StackOffset, IDebugType Type)
        {
            public abstract bool IsLocal { get; }
        }
        public sealed record class ArgStackVariable : StackVariable
        {
            public ArgStackVariable(string Name, LocalVarOffset StackOffset, IDebugType Type) : base(Name, StackOffset, Type)
            {
            }
            public override bool IsLocal => false;
        }
        public sealed record class LocalStackVariable : StackVariable
        {
            public LocalStackVariable(string Name, LocalVarOffset StackOffset, IDebugType Type) : base(Name, StackOffset, Type)
            {
            }
            public override bool IsLocal => true;
        }
        
        public readonly ImmutableArray<StackVariable> Variables;

        public VariableTable(ImmutableArray<StackVariable> variables)
        {
            Variables = variables;
        }

        public IEnumerable<ArgStackVariable> Args => Variables.OfType<ArgStackVariable>();
		public IEnumerable<StackVariable> Locals => Variables.OfType<LocalStackVariable>();
		public int CountArgs => Args.Count();
		public int CountLocals => Locals.Count();
	}


	public interface IDebugType
	{
		string Name { get; }
		string ReadValue(MemoryLocation location, Runtime runtime);
	}

    public sealed class DebugTypeINT : IDebugType
    {
        public static readonly DebugTypeINT Instance = new (); 
        public string Name => "INT";
        public string ReadValue(MemoryLocation location, Runtime runtime) => runtime.LoadINT(location).ToString();
    }
    public sealed class DebugTypeUnknown : IDebugType
    {
        public DebugTypeUnknown(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public string Name { get; }
        public string ReadValue(MemoryLocation location, Runtime runtime) => Name;
    }
}
