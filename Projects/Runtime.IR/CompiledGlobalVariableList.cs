using Runtime.IR.RuntimeTypes;
using System;
using System.Collections.Immutable;
using System.IO;

namespace Runtime.IR
{
    public sealed class CompiledGlobalVariableList
	{
		public sealed class Variable
		{
			public readonly string Name;
			public readonly ushort Offset;
			public readonly IRuntimeType Type;

            public Variable(string name, ushort offset, IRuntimeType type)
            {
                Name = name ?? throw new ArgumentNullException(nameof(name));
                Offset = offset;
                Type = type ?? throw new ArgumentNullException(nameof(type));
            }
        }
		public readonly string Name;
		public readonly ushort Area;
		public readonly ushort Size;
		public readonly ImmutableArray<Variable>? VariableTable;
        public CompiledGlobalVariableList(string name, ushort area, ushort size, ImmutableArray<Variable>? variableTable)
        {
            Name = name;
            Area = area;
            Size = size;
            VariableTable = variableTable;
        }
    }
}
