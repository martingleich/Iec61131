using Compiler.Types;
using Runtime.IR.RuntimeTypes;

namespace Compiler.CodegenIR
{
    public sealed class RuntimeTypeFactory
	{
		public SystemScope SystemScope;

        public RuntimeTypeFactory(SystemScope systemScope)
        {
            SystemScope = systemScope ?? throw new System.ArgumentNullException(nameof(systemScope));
        }

        public IRuntimeType GetRuntimeType(IType type)
		{
            if (type is BuiltInType builtInType)
                return builtInType.GetRuntimeType(this);
            else if (type is ArrayType arrayType)
                return arrayType.GetRuntimeType(this);
            else
                return new RuntimeTypeUnknown(type.Code, type.LayoutInfo.Size);
		}
	}
}
