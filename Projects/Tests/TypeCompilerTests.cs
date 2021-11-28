using Compiler;
using Compiler.Messages;
using Compiler.Scopes;
using Compiler.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Tests
{
	using static ErrorTestHelper;
	public sealed class TypeCompilerTests
	{
		private static readonly StructuredTypeSymbol MyType = new(
			default, false, "Test".ToCaseInsensitive(), "MyType".ToCaseInsensitive(), SymbolSet<FieldVariableSymbol>.Empty, new LayoutInfo(23, 8));
		private sealed class TypeSetScope : AInnerScope<IScope>
		{
			private readonly SymbolSet<ITypeSymbol> Types;

			public TypeSetScope(SymbolSet<ITypeSymbol> types, IScope outerScope) : base(outerScope)
			{
				Types = types;
			}

			public override ErrorsAnd<ITypeSymbol> LookupType(CaseInsensitiveString identifier, SourcePosition sourcePosition) =>
				Types.TryGetValue(identifier, out var type)
				? ErrorsAnd.Create(type)
				: base.LookupType(identifier, sourcePosition);
		}

		private static void AssertTypeCompiler(string input, Action<IType> check, params Action<IMessage>[] errorChecks)
		{
			var rootScope = new RootScope(new SystemScope("Test".ToCaseInsensitive()));
			var naiveScope = new TypeSetScope(SymbolSet.Create<ITypeSymbol>(MyType), rootScope);
			var source = ParserTestHelper.ParseType(input);
			var bag = new MessageBag();
			var bound = Compiler.TypeCompiler.MapComplete(naiveScope, source, bag);
			ErrorTestHelper.ExactlyMessages(errorChecks)(bag);
			check(bound);
		}

		[Theory]
		[InlineData ("INT", "Int")]
		[InlineData ("BOOL", "Bool")]
		[InlineData ("ARRAY[0..1] OF LREAL", "ARRAY[0..1] OF LReal")]
		[InlineData ("ARRAY[0..1,2..5] OF DATE", "ARRAY[0..1, 2..5] OF Date")]
		[InlineData ("ARRAY[0..0] OF LREAL", "ARRAY[0..0] OF LReal")]
		[InlineData ("ARRAY[0..-1] OF LREAL", "ARRAY[0..-1] OF LReal")]
		[InlineData ("MyType", "MyType")]
		[InlineData ("STRING[17]", "STRING[17]")]
		[InlineData ("STRING", "STRING[80]")]
		[InlineData ("STRING[SIZEOF(MyType)]", "STRING[23]")]
		[InlineData ("POINTER TO BYTE", "POINTER TO Byte")]
		[InlineData ("POINTER TO POINTER TO SINT", "POINTER TO POINTER TO SInt")]
		public void TypeCompiler(string str, string expected)
		{
			AssertTypeCompiler(str, bound => Assert.Equal(expected, bound.Code));
		}

		[Fact]
		public void Error_ArrayInvalidRanges()
		{
			AssertTypeCompiler("ARRAY[5..2] OF Int", t => Assert.Equal("ARRAY[5..5] OF Int", t.Code), ErrorOfType<InvalidArrayRangesMessage>());
		}
		
		[Fact]
		public void Error_BadUpperBound()
		{
			AssertTypeCompiler("ARRAY[5..TRUE] OF Int", t => Assert.Equal("ARRAY[5..5] OF Int", t.Code), ErrorOfType<TypeIsNotConvertibleMessage>());
		}
		[Fact]
		public void Error_BadLowerBound()
		{
			AssertTypeCompiler("ARRAY[TRUE..2] OF Int", t => Assert.Equal("ARRAY[2..2] OF Int", t.Code), ErrorOfType<TypeIsNotConvertibleMessage>());
		}
		[Fact]
		public void Error_BadLowerAndUpperBound()
		{
			AssertTypeCompiler("ARRAY[TRUE..FALSE] OF Int", t => Assert.Equal("ARRAY[0..0] OF Int", t.Code), ErrorOfType<TypeIsNotConvertibleMessage>(), ErrorOfType<TypeIsNotConvertibleMessage>());
		}
	}
}
