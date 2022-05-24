using System;
using Compiler;

namespace Compiler.CodegenIR
{

	public sealed partial class CodegenIR
	{
		private sealed class VariableAddressableVisitor : IVariableSymbol.IVisitor<IAddressable>
		{
			private readonly CodegenIR CodeGen;

			public VariableAddressableVisitor(CodegenIR codeGen)
			{
				CodeGen = codeGen ?? throw new ArgumentNullException(nameof(codeGen));
			}

			private GeneratorT Generator => CodeGen.Generator;

			public IAddressable Visit(LocalVariableSymbol localVariableSymbol) => Generator.LocalVariable(localVariableSymbol);
			public IAddressable Visit(InlineLocalVariableSymbol inlineLocalVariableSymbol) => Generator.LocalVariable(inlineLocalVariableSymbol);
			public IAddressable Visit(ParameterVariableSymbol parameterVariableSymbol)
			{
				var parameter = Generator.Parameter(parameterVariableSymbol);
				if (parameterVariableSymbol.Kind == ParameterKind.InOut)
					return new PointerVariableAddressable(parameter, parameterVariableSymbol.Type.LayoutInfo.Size);
				else
					return parameter;
			}
			public IAddressable Visit(GlobalVariableSymbol globalVariableSymbol) =>	Generator.GlobalVariable(globalVariableSymbol);
			public IAddressable Visit(FieldVariableSymbol fieldVariableSymbol) => Generator.GetElementAddressableField(CodeGen.Generator.ThisReference!, fieldVariableSymbol);
			public IAddressable Visit(FunctionVariableSymbol functionVariableSymbol) => throw new NotImplementedException("Cannot read FunctionVariableSymbol");

			#region Not addressable
			public IAddressable Visit(ErrorVariableSymbol errorVariableSymbol) => throw new InvalidOperationException();
			public IAddressable Visit(EnumVariableSymbol enumVariableSymbol) => throw new InvalidOperationException();
			#endregion
		}
	}
}
