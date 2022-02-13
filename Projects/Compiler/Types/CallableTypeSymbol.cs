using System.Linq;

namespace Compiler.Types
{
	public static class CallableTypeSymbol
	{
		public static IType GetReturnType(this ICallableTypeSymbol self) => self.Parameters.TryGetValue(self.Name, out var returnParam)
			? returnParam.Type
			: NullType.Instance;
		public static int GetParameterCountWithoutReturn(this ICallableTypeSymbol self) => self.GetReturnType() is NullType
			? self.Parameters.Count
			: self.Parameters.Count - 1;
	}
	
}