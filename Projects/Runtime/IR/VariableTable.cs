using Runtime.IR.RuntimeTypes;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Runtime.IR
{
    public sealed class VariableTable
	{
		public abstract record class StackVariable(string Name, LocalVarOffset StackOffset, IRuntimeType Type)
        {
            public abstract bool IsLocal { get; }
        }
        public sealed record class ArgStackVariable : StackVariable
        {
            public ArgStackVariable(string Name, LocalVarOffset StackOffset, IRuntimeType Type) : base(Name, StackOffset, Type)
            {
            }
            public override bool IsLocal => false;
        }
        public sealed record class LocalStackVariable : StackVariable
        {
            public LocalStackVariable(string Name, LocalVarOffset StackOffset, IRuntimeType Type) : base(Name, StackOffset, Type)
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
}
