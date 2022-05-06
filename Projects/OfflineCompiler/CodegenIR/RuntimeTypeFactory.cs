using Compiler;
using Compiler.Types;
using Runtime.IR.RuntimeTypes;

namespace OfflineCompiler
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
            if(TypeRelations.IsIdentical(type, SystemScope.BuiltInTypeTable.SInt))
                return RuntimeTypeSINT.Instance;
            else if(TypeRelations.IsIdentical(type, SystemScope.BuiltInTypeTable.Int))
                return RuntimeTypeINT.Instance;
            else if(TypeRelations.IsIdentical(type, SystemScope.BuiltInTypeTable.DInt))
                return RuntimeTypeDINT.Instance;
            else if(TypeRelations.IsIdentical(type, SystemScope.BuiltInTypeTable.LInt))
                return RuntimeTypeLINT.Instance;
            else if(TypeRelations.IsIdentical(type, SystemScope.BuiltInTypeTable.Real))
                return RuntimeTypeREAL.Instance;
            else if(TypeRelations.IsIdentical(type, SystemScope.BuiltInTypeTable.LReal))
                return RuntimeTypeLREAL.Instance;
            else if(TypeRelations.IsIdentical(type, SystemScope.BuiltInTypeTable.Bool))
                return RuntimeTypeBOOL.Instance;
            else
                return new RuntimeTypeUnknown(type.Code);
		}
	}
}
