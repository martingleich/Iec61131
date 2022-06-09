using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Compiler;
using Compiler.Types;
using Runtime.IR;
using IR = Runtime.IR;
using IRStmt = Runtime.IR.Statements;

namespace Compiler.CodegenIR
{
    public sealed partial class CodegenIR
	{
        public sealed class GeneratorT
		{
			private readonly CodegenIR CodeGen;
			public GeneratorT(CodegenIR codeGen)
			{
				CodeGen = codeGen ?? throw new ArgumentNullException(nameof(codeGen));
				if (codeGen._stackAllocator.ThisVariableOffset is IR.LocalVarOffset thisOffset)
					ThisReference = new LocalVariable("this", IR.Type.Pointer, thisOffset);
			}

			private readonly List<IR.IStatement> _statements = new();
			private int _nextTempId = 0;
			private int _nextLabelId = 0;
			public readonly IAddressable? ThisReference;

			public int InstructionId => _statements.Count;

			public IRStmt.Label DeclareLabel() => new($"{_nextLabelId++}");
			public LocalVariable DeclareTemp(IR.Type type)
			{
				var id = $"tmp{_nextTempId++}";
				var offset = CodeGen._stackAllocator.AllocTemp(type);
				return new(id, type, offset);
			}

			public LocalVariable DeclareTemp(IType type) => DeclareTemp(TypeFromIType(type));
			public LocalVariable DeclareTemp(IR.Type type, IReadable value)
			{
				var variable = DeclareTemp(type);
				variable.Assign(CodeGen, value);
				return variable;
			}
			public LocalVariable DeclareTemp(IType type, IReadable value) => DeclareTemp(TypeFromIType(type), value);

			public void IL(IR.IStatement statement)
			{
				_statements.Add(statement);
			}
			public void IL_WriteDeref(IReadable value, LocalVariable variable)
			{
				_statements.Add(new IRStmt.WriteDerefValue(value.GetExpression(), variable.Offset, variable.Type.Size));
			}
			public void IL_Comment(string comment)
			{
				IL(new IRStmt.Comment(comment));
			}
			public void IL_Jump(IRStmt.Label target)
			{
				IL(new IRStmt.Jump(target));
			}
			public void IL_Jump_IfNot(LocalVariable variable, IRStmt.Label target)
			{
				IL(new IRStmt.JumpIfNot(variable.Offset, target));
			}
			public void IL_Label(IRStmt.Label label)
			{
				label.StatementId = _statements.Count;
				IL(label);
			}

			public LocalVariable IL_SimpleCallAsVariable(IR.Type type, IR.PouId symbolId, params LocalVariable[] args)
				=> CodeGen.LoadValueAsVariable(type, IL_SimpleCall(null, type, symbolId, args));
			public IReadable IL_SimpleCall(LocalVariable? targetVar, IR.Type type, IR.PouId symbolId, params LocalVariable[] args)
			{
				var tmp = targetVar ?? DeclareTemp(type);
				IL(new IRStmt.StaticCall(symbolId,
					args.Select(arg => arg.Offset).ToImmutableArray(),
					ImmutableArray.Create(tmp.Offset)));
				return tmp;
			}
			public IReadable IL_SimpleCall(LocalVariable? targetVar, FunctionVariableSymbol variable, params LocalVariable[] args)
				=> IL_SimpleCall(targetVar, CodegenIR.TypeFromIType(variable.Type.GetReturnType()), PouIdFromSymbol(variable), args);

			public IAddressable GetElementAddressableField(IAddressable baseReference, FieldVariableSymbol field)
				=> baseReference.GetElementAddressable(CodeGen, new ElementAddressable.Element.Field(field), CodegenIR.TypeFromIType(field.Type).Size);

			public LocalVariable LocalVariable(ILocalVariableSymbol localVariable)
			{
				var (offset, irType) = CodeGen._stackAllocator.GetLocalInfo(localVariable);
				return new LocalVariable(localVariable.Name.Original, irType, offset);
			}
			public LocalVariable Parameter(ParameterVariableSymbol parameterVariable)
			{
				var (offset, irType) = CodeGen._stackAllocator.GetParameterInfo(parameterVariable);
				return new LocalVariable(parameterVariable.Name.Original, irType, offset);
			}
			public GlobalVariable GlobalVariable(GlobalVariableSymbol globalVariable)
			{
				var location = CodeGen.GlobalVariableAllocationTable.GetAreaOffset(globalVariable);
				return new GlobalVariable(globalVariable.UniqueName.ToString(), location, globalVariable.Type.LayoutInfo.Size);
			}

			public ImmutableArray<IStatement> GetStatements() => _statements.ToImmutableArray();
		}
	}
}
