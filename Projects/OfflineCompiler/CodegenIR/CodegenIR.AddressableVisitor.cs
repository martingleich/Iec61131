using System;
using System.Collections.Immutable;
using System.Linq;
using Compiler;
using Compiler.Types;
using IR = Runtime.IR;

namespace OfflineCompiler
{
	public sealed partial class CodegenIR
	{
		private sealed class AddressableVisitor : IBoundExpression.IVisitor<IAddressable>
		{
			private readonly CodegenIR CodeGen;

			public AddressableVisitor(CodegenIR codeGen)
			{
				CodeGen = codeGen ?? throw new ArgumentNullException(nameof(codeGen));
			}

			public IAddressable Visit(VariableBoundExpression variableBoundExpression) => variableBoundExpression.Variable.Accept(CodeGen._variableAddressableVisitor);

			public IAddressable Visit(ArrayIndexAccessBoundExpression arrayIndexAccessBoundExpression)
			{
				var @base = CodeGen.LoadAddressable(arrayIndexAccessBoundExpression.Base);
				var values = ImmutableArray.CreateRange(arrayIndexAccessBoundExpression.Indices, CodeGen.LoadValueAsVariable);
				var arrayType = (ArrayType)arrayIndexAccessBoundExpression.Base.Type;
				return @base.GetElementAddressable(
					CodeGen,
					new ElementAddressable.Element.ArrayIndex(values, arrayType), arrayType.BaseType.LayoutInfo.Size);
			}
			public IAddressable Visit(FieldAccessBoundExpression fieldAccessBoundExpression)
			{
				var @base = CodeGen.LoadAddressable(fieldAccessBoundExpression.BaseExpression);
				return CodeGen.Generator.GetElementAddressableField(@base, fieldAccessBoundExpression.Field);
			}
			public IAddressable Visit(PointerIndexAccessBoundExpression pointerIndexAccessBoundExpression)
			{
				var @base = CodeGen.LoadAddressable(pointerIndexAccessBoundExpression.Base);
				var index = ImmutableArray.CreateRange(pointerIndexAccessBoundExpression.Indices, CodeGen.LoadValueAsVariable).Single();
				var pointerType = (PointerType)pointerIndexAccessBoundExpression.Type;
				return @base.GetElementAddressable(CodeGen,
					new ElementAddressable.Element.PointerIndex(index, pointerType.BaseType.LayoutInfo.Size), pointerType.BaseType.LayoutInfo.Size);
			}
			public IAddressable Visit(DerefBoundExpression derefBoundExpression)
			{
				var value = CodeGen.LoadValueAsVariable(derefBoundExpression.Value);
				return new PointerVariableAddressable(value, derefBoundExpression.Type.LayoutInfo.Size);
			}

			#region Not assignable
			public IAddressable Visit(LiteralBoundExpression literalBoundExpression) => throw new InvalidOperationException();
			public IAddressable Visit(SizeOfTypeBoundExpression sizeOfTypeBoundExpression) => throw new InvalidOperationException();
			public IAddressable Visit(ImplicitEnumToBaseTypeCastBoundExpression implicitEnumCastBoundExpression) => throw new InvalidOperationException();
			public IAddressable Visit(BinaryOperatorBoundExpression binaryOperatorBoundExpression) => throw new InvalidOperationException();
			public IAddressable Visit(ImplicitPointerTypeCastBoundExpression implicitPointerTypeCaseBoundExpression) => throw new InvalidOperationException();
			public IAddressable Visit(ImplicitCastBoundExpression implicitArithmeticCaseBoundExpression) => throw new InvalidOperationException();
			public IAddressable Visit(UnaryOperatorBoundExpression unaryOperatorBoundExpression) => throw new InvalidOperationException();
			public IAddressable Visit(PointerDiffrenceBoundExpression pointerDiffrenceBoundExpression) => throw new InvalidOperationException();
			public IAddressable Visit(ImplicitAliasToBaseTypeCastBoundExpression aliasToBaseTypeCastBoundExpression) => throw new InvalidOperationException();
			public IAddressable Visit(ImplicitErrorCastBoundExpression implicitErrorCastBoundExpression) => throw new InvalidOperationException();
			public IAddressable Visit(ImplicitAliasFromBaseTypeCastBoundExpression implicitAliasFromBaseTypeCastBoundExpression) => throw new InvalidOperationException();
			public IAddressable Visit(ImplicitDiscardBoundExpression implicitDiscardBoundExpression) => throw new InvalidOperationException();
			public IAddressable Visit(CallBoundExpression callBoundExpression) => throw new InvalidOperationException();
			public IAddressable Visit(InitializerBoundExpression initializerBoundExpression) => throw new InvalidOperationException();
			public IAddressable Visit(PointerOffsetBoundExpression pointerOffsetBoundExpression) => throw new InvalidOperationException();
			#endregion
		}
	
		private readonly AddressableVisitor _addressableVisitor;
		private IAddressable LoadAddressable(IBoundExpression expression) => expression.Accept(_addressableVisitor);
		private IWritable LoadWritable(IBoundExpression expression) => LoadAddressable(expression).ToWritable(this);
		private LocalVariable LoadAddressAsVariable(IBoundExpression expression)
		{
			var value = expression.Accept(_addressableVisitor).ToPointerValue(this);
			if (value is LocalVariable variable)
				return variable;
			else
				return Generator.DeclareTemp(IR.Type.Pointer, value);
		}
	}
}
