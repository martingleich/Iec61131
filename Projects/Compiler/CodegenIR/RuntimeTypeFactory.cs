using Compiler.Types;
using Runtime.IR.RuntimeTypes;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Compiler.CodegenIR
{
    public sealed class RuntimeTypeFactoryFromType
	{
        private readonly Dictionary<string, RuntimeTypeStructured> _complexTypes = new();

        public ImmutableArray<RuntimeTypeStructured> GetTypes()
        {
            return _complexTypes.Values.ToImmutableArray();
        }

        public IRuntimeType GetRuntimeType(IType type)
		{
            if (type is BuiltInType builtInType)
                return builtInType.GetRuntimeType(this);
            else if (type is ArrayType arrayType)
                return arrayType.GetRuntimeType(this);
            else if (type is IStructuredTypeSymbol structuredType)
                return GetRuntimeType(structuredType);
            else
                return new RuntimeTypeUnknown(type.Code, type.LayoutInfo.Size);
		}

        private RuntimeTypeStructured GetRuntimeType(IStructuredTypeSymbol structuredType)
        {
            var name = structuredType.Name.Original;
            if (!_complexTypes.TryGetValue(name, out var result))
            {
                var size = structuredType.LayoutInfo.Size;
                var properties = structuredType.Fields
                    .Select(f => new RuntimeTypeStructured.Property(f.Name.Original, GetRuntimeType(f.Type), f.Offset))
                    .OrderBy(x => x.Name)
                    .ToImmutableArray();
                result = _complexTypes[name] = new RuntimeTypeStructured(name, size, properties);
            }
            return result;
        }
    }
}
