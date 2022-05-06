using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Compiler;
using Compiler.Types;
using Runtime.IR;
using StandardLibraryExtensions;
using IR = Runtime.IR;
using IRExpr = Runtime.IR.Expressions;
using IRStmt = Runtime.IR.Statements;

namespace OfflineCompiler
{
	public interface IReadable
	{
		IR.IExpression GetExpression();
	}
	public interface IWritable
	{
		void Assign(CodegenIR codegen, IReadable value);
	}
	public interface IAddressable
	{
		IReadable ToReadable(CodegenIR codegen);
		IWritable ToWritable(CodegenIR codegen);
		IReadable ToPointerValue(CodegenIR codegen);

		IAddressable GetElementAddressable(CodegenIR codegen, ElementAddressable.Element element, int derefSize);
	}

	public sealed class LocalVariable : IReadable, IWritable, IAddressable
	{
		public readonly string Id;
		public readonly IR.Type Type;
		public readonly IR.LocalVarOffset Offset;

		public LocalVariable(string id, IR.Type type, IR.LocalVarOffset offset)
		{
			Id = id;
			Type = type;
			Offset = offset;
		}
		public void Assign(CodegenIR codegen, IReadable value)
		{
			codegen.Generator.IL(new IRStmt.WriteValue(
				value.GetExpression(),
				Offset,
				Type.Size));
		}
		public IReadable ToReadable(CodegenIR codegen) => this;
		public IWritable ToWritable(CodegenIR codegen) => this;
		public IReadable ToPointerValue(CodegenIR codegen) => new ElementAddressable(new IRExpr.AddressExpression.BaseStackVar(Offset), Type.Size).ToPointerValue(codegen);
		public IR.IExpression GetExpression() => new IRExpr.LoadValueExpression(Offset);
		public override string ToString() => Id;

		public IAddressable GetElementAddressable(CodegenIR codegen, ElementAddressable.Element element, int derefSize)
			=> new ElementAddressable(new IRExpr.AddressExpression.BaseStackVar(Offset), ImmutableArray.Create(element), derefSize);
	}
	public sealed record JustReadable(IR.IExpression Expression) : IReadable
	{
		public IR.IExpression GetExpression() => Expression;
		public override string? ToString() => Expression.ToString();
	}
	public sealed class Deref : IReadable, IWritable
	{
		private readonly LocalVariable Pointer;
		private readonly int Size;

		public Deref(LocalVariable pointer, int size)
		{
			Pointer = pointer;
			Size = size;
		}

		public void Assign(CodegenIR codegen, IReadable value)
		{
			var irValue = value.GetExpression();
			codegen.Generator.IL(new IRStmt.WriteDerefValue(irValue, Pointer.Offset, Size));
		}

		public IR.IExpression GetExpression() => new IRExpr.DerefExpression(Pointer.Offset);
		public override string ToString() => $"{Pointer}^";
	}
	public sealed class GlobalVariable : IAddressable
	{
		public readonly string Id;
		public readonly MemoryLocation Location;
		public readonly int Size;

		public GlobalVariable(string id, MemoryLocation location, int size)
		{
			Id = id;
			Location = location;
			Size = size;
		}

		private Deref Deref(CodegenIR codegen)
		{
			var ptr = ToPointerValue(codegen);
			var tmp = codegen.Generator.DeclareTemp(IR.Type.Pointer, ptr);
			return new Deref(tmp, Size);
		}

		public IReadable ToPointerValue(CodegenIR codegen) => new JustReadable(IRExpr.LiteralExpression.FromMemoryLocation(Location));
		public IReadable ToReadable(CodegenIR codegen) => Deref(codegen);
		public IWritable ToWritable(CodegenIR codegen) => Deref(codegen);
		public override string ToString() => Id;

		public IAddressable GetElementAddressable(CodegenIR codegen, ElementAddressable.Element element, int derefSize)
		{
			var ptrValue = ToPointerValue(codegen);
			var baseVar = codegen.Generator.DeclareTemp(IR.Type.Pointer, ptrValue);
			return new ElementAddressable(new IRExpr.AddressExpression.BaseDerefStackVar(baseVar.Offset), ImmutableArray.Create(element), derefSize);
		}
	}
	public sealed class PointerVariableAddressable : IAddressable
	{
		public readonly LocalVariable Variable;
		public readonly int Size;

		public PointerVariableAddressable(LocalVariable variable, int size)
		{
			Variable = variable ?? throw new ArgumentNullException(nameof(variable));
			Size = size;
		}

		public IReadable ToReadable(CodegenIR codegen) => new Deref(Variable, Size);
		public IWritable ToWritable(CodegenIR codegen) => new Deref(Variable, Size);
		public IReadable ToPointerValue(CodegenIR codegen) => Variable;
		public IAddressable GetElementAddressable(CodegenIR codegen, ElementAddressable.Element element, int derefSize)
			=> new ElementAddressable(new IRExpr.AddressExpression.BaseDerefStackVar(Variable.Offset), ImmutableArray.Create(element), derefSize);
		public override string ToString() => $"{Variable}^";
	}
	public sealed class ElementAddressable : IAddressable
	{
		public abstract class Element
		{
			public class Field : Element
			{
				public readonly FieldVariableSymbol Symbol;

				public Field(FieldVariableSymbol symbol)
				{
					Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
				}

				public override void AddTo(IRExpr.AddressExpression.Builder builder) => builder.Add(new IRExpr.AddressExpression.ElementOffset((ushort)Symbol.Offset));

				public override string ToString() => $"fieldoffset({Symbol.Name})";
			}
			public class ArrayIndex : Element
			{
				public readonly ImmutableArray<LocalVariable> Indices;
				public readonly ArrayType ArrayType;

				public ArrayIndex(ImmutableArray<LocalVariable> indices, ArrayType arrayType)
				{
					Indices = indices;
					ArrayType = arrayType;
				}

				public override void AddTo(IRExpr.AddressExpression.Builder builder)
				{
					var totalSize = ArrayType.BaseType.LayoutInfo.Size;
					for (int i = Indices.Length - 1; i >= 0; --i)
					{
						builder.Add(new IRExpr.AddressExpression.ElementCheckedArray(
							ArrayType.Ranges[i].LowerBound,
							ArrayType.Ranges[i].UpperBound,
							Indices[i].Offset,
							totalSize));
						totalSize *= ArrayType.Ranges[i].Size;
					}
				}

				public override string ToString() => $"arrayoffset({ArrayType.Ranges.Zip(Indices, (r, i) => $"{i}@{r.LowerBound}..{r.UpperBound}").DelimitWith(" + ")}) * SIZEOF({ArrayType.BaseType})";
			}
			public class PointerIndex : Element
			{
				public readonly LocalVariable Index;
				public readonly int DerefSize;

				public PointerIndex(LocalVariable index, int size)
				{
					Index = index ?? throw new ArgumentNullException(nameof(index));
					DerefSize = size;
				}

				public override void AddTo(IRExpr.AddressExpression.Builder builder)
				{
					builder.Add(new IRExpr.AddressExpression.ElementUncheckedArray(Index.Offset, DerefSize));
				}

				public override string ToString() => $"{Index}*{DerefSize}";
			}

			public abstract void AddTo(IRExpr.AddressExpression.Builder builder);
		}
		public readonly IRExpr.AddressExpression.IBase Base;
		public readonly ImmutableArray<Element> Elements;
		public readonly int DerefSize;

		public ElementAddressable(IRExpr.AddressExpression.IBase @base, int derefSize) : this(@base, ImmutableArray<Element>.Empty, derefSize)
		{
		}
		public ElementAddressable(IRExpr.AddressExpression.IBase @base, ImmutableArray<Element> elements, int derefSize)
		{
			Base = @base ?? throw new ArgumentNullException(nameof(@base));
			Elements = elements;
			DerefSize = derefSize;
		}

		private Deref Deref(CodegenIR codegen)
		{
			var ptr = ToPointerValue(codegen);
			var tmp = codegen.Generator.DeclareTemp(IR.Type.Pointer, ptr);
			return new Deref(tmp, DerefSize);
		}
		public IReadable ToReadable(CodegenIR codegen) => Deref(codegen);
		public IWritable ToWritable(CodegenIR codegen) => Deref(codegen);
		public IReadable ToPointerValue(CodegenIR codegen)
		{
			var builder = new IRExpr.AddressExpression.Builder(Base);
			foreach (var elem in Elements)
				elem.AddTo(builder);
			return new JustReadable(builder.GetAddressExpression());
		}
		public override string ToString() => $"{Base} + {Elements.DelimitWith(" + ")}";

		public IAddressable GetElementAddressable(CodegenIR codegen, Element element, int derefSize)
			=> new ElementAddressable(Base, Elements.Add(element), derefSize);
	}

	public sealed partial class CodegenIR
	{
		public readonly BreakpointMapBuilder BreakpointFactory = new();
		public readonly RuntimeTypeFactory RuntimeTypeFactory;
		public readonly GeneratorT Generator;
		public readonly IR.PouId Id;
        public readonly GlobalVariableAllocationTable GlobalVariableAllocationTable;

        public static IR.PouId PouIdFromSymbol(FunctionVariableSymbol variable)
			=> new(variable.UniqueName.ToString().ToUpperInvariant());

		public static IR.PouId PouIdFromSymbol(ICallableTypeSymbol callableSymbol)
			=> new(callableSymbol.UniqueName.ToString().ToUpperInvariant());
		public static IR.Type TypeFromIType(IType type) => new(type.LayoutInfo.Size);

		public CodegenIR(BoundPou pou, SystemScope systemScope, GlobalVariableAllocationTable globalVariableAllocationTable)
		{
			RuntimeTypeFactory = new RuntimeTypeFactory(systemScope);
            GlobalVariableAllocationTable = globalVariableAllocationTable ?? throw new ArgumentNullException(nameof(globalVariableAllocationTable));

			Id = PouIdFromSymbol(pou.CallableSymbol);
			_stackAllocator = new(this, pou.CallableSymbol);

			Generator = new(this);
			_loadVariableExpressionVisitor = new(this);
			_loadValueExpressionVisitor = new(this);
			_statementVisitor = new(this);
			_variableAddressableVisitor = new(this);
			_addressableVisitor = new(this);
        }
		
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
				=> CodeGen.LoadValueAsVariable(type, IL_SimpleCall(type, symbolId, args));
			public IReadable IL_SimpleCall(IR.Type type, IR.PouId symbolId, params LocalVariable[] args)
			{
				var tmp = DeclareTemp(type);
				IL(new IRStmt.StaticCall(symbolId,
					args.Select(arg => arg.Offset).ToImmutableArray(),
					ImmutableArray.Create(tmp.Offset)));
				return tmp;
			}
			public IReadable IL_SimpleCall(FunctionVariableSymbol variable, params LocalVariable[] args)
				=> IL_SimpleCall(CodegenIR.TypeFromIType(variable.Type.GetReturnType()), PouIdFromSymbol(variable), args);

			public IAddressable GetElementAddressableField(IAddressable baseReference, FieldVariableSymbol field)
				=> baseReference.GetElementAddressable(CodeGen, new ElementAddressable.Element.Field(field), CodegenIR.TypeFromIType(field.Type).Size);

			public LocalVariable LocalVariable(ILocalVariableSymbol localVariable)
			{
				var (offset, irType) = CodeGen._stackAllocator.AllocStackLocal(localVariable);
				return new LocalVariable(localVariable.Name.Original, irType, offset);
			}
			public LocalVariable Parameter(ParameterVariableSymbol parameterVariable)
			{
				var (offset, irType) = CodeGen._stackAllocator.AllocParameter(parameterVariable);
				return new LocalVariable(parameterVariable.Name.Original, irType, offset);
			}
			public GlobalVariable GlobalVariable(GlobalVariableSymbol globalVariable)
			{
				var location = CodeGen.GlobalVariableAllocationTable.GetAreaOffset(globalVariable);
				return new GlobalVariable(globalVariable.UniqueName.ToString(), location, globalVariable.Type.LayoutInfo.Size);
			}

			public ImmutableArray<IR.IStatement> GetStatements() => _statements.ToImmutableArray();
		}

		public CompiledPou GetGeneratedCode(SourceMap.SingleFile? sourceMap)
		{
			var statments = Generator.GetStatements();
            var breakpointMap = sourceMap != null ? BreakpointFactory.ToBreakpointMap(sourceMap) : null;
			var variableTable = _stackAllocator.GetVariableTable();
            return new(
                Id,
                _stackAllocator.TotalMemory,
                _stackAllocator.InputArgs,
                _stackAllocator.OutputArgs,
                statments)
            {
				BreakpointMap = breakpointMap,
				OriginalPath = sourceMap?.FullPath,
				VariableTable = variableTable
            };
		}

		public void CompileInitials(OrderedSymbolSet<LocalVariableSymbol> localVariables)
		{
			foreach (var local in localVariables)
			{
				if (local.InitialValue is IBoundExpression initial)
				{
					var value = initial.Accept(_loadValueExpressionVisitor);
					Generator.LocalVariable(local).Assign(this, value);
				}
			}
		}
	}
}
