using System;
using System.Collections.Immutable;
using Runtime.IR.Expressions;

namespace Compiler.CodegenIR
{
	public sealed partial class CodegenIR
	{
		private readonly LoadLiteralValueVisitor _loadLiteralValue;
		public sealed class LoadLiteralValueVisitor : ILiteralValue.IVisitor<IReadable>
		{
			public CodegenIR CodeGen;
            public LoadLiteralValueVisitor(CodegenIR codeGen)
            {
                CodeGen = codeGen ?? throw new ArgumentNullException(nameof(codeGen));
            }

            public IReadable Visit(TimeLiteralValue timeLiteralValue) => new JustReadable(LiteralExpression.Signed32(timeLiteralValue.Value.Milliseconds));
			public IReadable Visit(LTimeLiteralValue lTimeLiteralValue) => new JustReadable(LiteralExpression.Signed64(lTimeLiteralValue.Value.Nanoseconds));
			public IReadable Visit(NullPointerLiteralValue nullPointerLiteralValue) => new JustReadable(LiteralExpression.NullPointer);
			public IReadable Visit(LRealLiteralValue lRealLiteralValue) => new JustReadable(LiteralExpression.Float64(lRealLiteralValue.Value));
			public IReadable Visit(RealLiteralValue realLiteralValue) => new JustReadable(LiteralExpression.Float32(realLiteralValue.Value));
			public IReadable Visit(EnumLiteralValue enumLiteralValue) => enumLiteralValue.InnerValue.Accept(this);
			public IReadable Visit(BooleanLiteralValue booleanLiteralValue) => new JustReadable(LiteralExpression.Bool(booleanLiteralValue.Value));
			public IReadable Visit(LIntLiteralValue lIntLiteralValue) => new JustReadable(LiteralExpression.Signed64(lIntLiteralValue.Value));
			public IReadable Visit(ULIntLiteralValue uLIntLiteralValue) => new JustReadable(LiteralExpression.Bits64(uLIntLiteralValue.Value));
			public IReadable Visit(DIntLiteralValue dIntLiteralValue) => new JustReadable(LiteralExpression.Signed32(dIntLiteralValue.Value));
			public IReadable Visit(UDIntLiteralValue uDIntLiteralValue) => new JustReadable(LiteralExpression.Bits32(uDIntLiteralValue.Value));
			public IReadable Visit(IntLiteralValue intLiteralValue) => new JustReadable(LiteralExpression.Signed16(intLiteralValue.Value));
			public IReadable Visit(UIntLiteralValue uIntLiteralValue) => new JustReadable(LiteralExpression.Bits16(uIntLiteralValue.Value));
			public IReadable Visit(USIntLiteralValue uSIntLiteralValue) => new JustReadable(LiteralExpression.Bits8(uSIntLiteralValue.Value));
			public IReadable Visit(SIntLiteralValue sIntLiteralValue) => new JustReadable(LiteralExpression.Signed8(sIntLiteralValue.Value));
            public IReadable Visit(ArrayLiteralValue arrayLiteralValue)
            {
				ImmutableArray<InitializerBoundExpression.ABoundElement> elements;
				if (arrayLiteralValue is ArrayLiteralValue.AllSameArrayLiteralValue allSame)
				{
					var element = new InitializerBoundExpression.ABoundElement.AllElements(new LiteralBoundExpression(null, allSame.Value));
					elements = ImmutableArray.Create<InitializerBoundExpression.ABoundElement>(element);
				}
				else
				{
					var elementsBuilder = ImmutableArray.CreateBuilder<InitializerBoundExpression.ABoundElement>();
					for (int i = 0; i < arrayLiteralValue.Type.ElementCount; ++i)
					{
						var value = arrayLiteralValue.GetElement(i);
						var element = new InitializerBoundExpression.ABoundElement.ArrayElement(new BoundConstantIntegerValue(null, i), new LiteralBoundExpression(null, value));
						elementsBuilder.Add(element);
					}
					elements = elementsBuilder.ToImmutable();
				}

				var initializer = new InitializerBoundExpression.ArrayInitializerBoundExpression(elements, arrayLiteralValue.Type, null);
				return CodeGen._loadValueExpressionVisitor.Visit(initializer, null);
            }
            public IReadable Visit(StructuredLiteralValue structuredLiteralValue)
            {
				var elementsBuilder = ImmutableArray.CreateBuilder<InitializerBoundExpression.ABoundElement>();
				foreach (var elem in structuredLiteralValue.Elements)
				{
					var field = structuredLiteralValue.Type.Fields[elem.Key];
					var element = new InitializerBoundExpression.ABoundElement.FieldElement(field, new LiteralBoundExpression(null, elem.Value));
					elementsBuilder.Add(element);
				}
				var elements = elementsBuilder.ToImmutable();
				var initializer = new InitializerBoundExpression.StructuredInitializerBoundExpression(elements, structuredLiteralValue.Type, null);
				return CodeGen._loadValueExpressionVisitor.Visit(initializer, null);
            }
			public IReadable Visit(UnknownLiteralValue unknownLiteralValue) => throw new InvalidOperationException();
        }
	}
}
