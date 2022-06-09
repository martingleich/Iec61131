using System;

namespace Compiler.CodegenIR
{

	public sealed partial class CodegenIR
	{
		private sealed class LoadVariableExpressionVisitor : IVariableSymbol.IVisitor<IReadable>
		{
			private readonly CodegenIR CodeGen;

			public LoadVariableExpressionVisitor(CodegenIR codeGen)
			{
				CodeGen = codeGen ?? throw new ArgumentNullException(nameof(codeGen));
			}

			public IReadable Visit(FieldVariableSymbol fieldVariableSymbol) => CodeGen._variableAddressableVisitor.Visit(fieldVariableSymbol).ToReadable(CodeGen);
			public IReadable Visit(GlobalVariableSymbol globalVariableSymbol) => CodeGen._variableAddressableVisitor.Visit(globalVariableSymbol).ToReadable(CodeGen);
			public IReadable Visit(LocalVariableSymbol localVariableSymbol) => CodeGen._variableAddressableVisitor.Visit(localVariableSymbol).ToReadable(CodeGen);
			public IReadable Visit(InlineLocalVariableSymbol inlineLocalVariableSymbol) => CodeGen._variableAddressableVisitor.Visit(inlineLocalVariableSymbol).ToReadable(CodeGen);
			public IReadable Visit(ParameterVariableSymbol parameterVariableSymbol) => CodeGen._variableAddressableVisitor.Visit(parameterVariableSymbol).ToReadable(CodeGen);
			public IReadable Visit(FunctionVariableSymbol functionVariableSymbol) => CodeGen._variableAddressableVisitor.Visit(functionVariableSymbol).ToReadable(CodeGen);

			public IReadable Visit(EnumVariableSymbol enumVariableSymbol) => enumVariableSymbol.Value.InnerValue.Accept(CodeGen._loadLiteralValue);
			public IReadable Visit(ErrorVariableSymbol errorVariableSymbol) => throw new InvalidOperationException();
		}
		private readonly LoadVariableExpressionVisitor _loadVariableExpressionVisitor;
	}
}
