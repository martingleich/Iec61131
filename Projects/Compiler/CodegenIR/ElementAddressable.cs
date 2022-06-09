using System;
using System.Collections.Immutable;
using System.Linq;
using Compiler.Types;
using StandardLibraryExtensions;
using IR = Runtime.IR;
using IRExpr = Runtime.IR.Expressions;

namespace Compiler.CodegenIR
{
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
}
