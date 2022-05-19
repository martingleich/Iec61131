using Compiler;
using Runtime.IR;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace OfflineCompiler
{
    public sealed class GlobalVariableAllocationTable
	{
		private readonly CaseInsensitiveString _moduleName;
		private readonly Dictionary<CaseInsensitiveString, (CompiledGlobalVariableList, Dictionary<CaseInsensitiveString, ushort>)> _lists;

        private GlobalVariableAllocationTable(CaseInsensitiveString moduleName, Dictionary<CaseInsensitiveString, (CompiledGlobalVariableList, Dictionary<CaseInsensitiveString, ushort>)> lists)
        {
            _moduleName = moduleName;
            _lists = lists ?? throw new System.ArgumentNullException(nameof(lists));
        }

        public static GlobalVariableAllocationTable Generate(ushort firstArea, BoundModuleInterface moduleInterface, RuntimeTypeFactory runtimeTypeFactory)
		{
            Dictionary<CaseInsensitiveString, (CompiledGlobalVariableList, Dictionary<CaseInsensitiveString, ushort>)> lists = new();
			ushort area = firstArea;
            foreach (var globalVarList in moduleInterface.GlobalVariableListSymbols.OrderBy(x => x.Name))
            {
				List<CompiledGlobalVariableList.Variable> symbols = new();
				var field = FieldLayout.Zero;
                foreach (var variable in globalVarList.Variables.OrderByDescending(x => x.Type.LayoutInfo.Alignment).ThenBy(x => x.Name))
                {
					field = field.NextField(variable.Type.LayoutInfo);
					var runtimeType = runtimeTypeFactory.GetRuntimeType(variable.Type);
					symbols.Add(new CompiledGlobalVariableList.Variable(variable.Name.Original, (ushort)field.Offset, runtimeType));
                }
				var compiled = new CompiledGlobalVariableList(
					globalVarList.Name.Original,
					area,
					(ushort)(field.Offset + field.Size),
					symbols.ToImmutableArray());
				var offsets = symbols.ToDictionary(v => v.Name.ToCaseInsensitive(), v => v.Offset);
				lists.Add(globalVarList.Name, (compiled, offsets));
				area++;
            }

			return new(moduleInterface.Name, lists);
		}

		public MemoryLocation GetAreaOffset(GlobalVariableSymbol variable)
		{
			if (variable.ModuleName != _moduleName)
				throw new System.ArgumentException();
			var (area, offsets) = _lists[variable.GvlName];
			return new (area.Area, offsets[variable.Name]);
		}

		public IEnumerable<CompiledGlobalVariableList> ToCompiledGvls() => _lists.Values.Select(v => v.Item1);
	}
}
