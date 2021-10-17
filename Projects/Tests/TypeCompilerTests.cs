using Compiler;
using Compiler.Messages;
using Compiler.Scopes;
using Compiler.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Tests
{
	using static ErrorTestHelper;
	public sealed class TypeCompilerTests
	{
		private static readonly SystemScope SystemScope = BindHelper.SystemScope;
		private static readonly StructuredTypeSymbol MyType = new(default, false, "MyType".ToCaseInsensitive(), SymbolSet<FieldVariableSymbol>.Empty, new LayoutInfo(23, 8));
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
			var naiveScope = new TypeSetScope(SymbolSet.Create<ITypeSymbol>(MyType), BindHelper.RootScope);
			var source = ParserTestHelper.ParseType(input);
			var bag = new MessageBag();
			var bound = Compiler.TypeCompiler.MapComplete(naiveScope, source, bag);
			ErrorTestHelper.ExactlyMessages(errorChecks)(bag);
			check(bound);
		}

		public static IEnumerable<object[]> TypeCompiler_Data() => TypeCompilerData_Typed().Select(v => new object[] { v.Item1, v.Item2 });
		public static IEnumerable<(string, IType)> TypeCompilerData_Typed()
		{
			yield return ("INT", SystemScope.Int);
			yield return ("BOOL", SystemScope.Bool);
			yield return ("ARRAY[0..1] OF LREAL", new ArrayType(SystemScope.LReal, ImmutableArray.Create(new ArrayType.Range(0, 1))));
			yield return ("ARRAY[0..1,2..5] OF DATE", new ArrayType(SystemScope.Date, ImmutableArray.Create(new ArrayType.Range(0, 1), new ArrayType.Range(2, 5))));
			yield return ("ARRAY[0..0] OF LREAL", new ArrayType(SystemScope.LReal, ImmutableArray.Create(new ArrayType.Range(0, 0))));
			yield return ("ARRAY[0..-1] OF LREAL", new ArrayType(SystemScope.LReal, ImmutableArray.Create(new ArrayType.Range(0, -1))));
			yield return ("MyType", new StructuredTypeSymbol(default, false, "MyType".ToCaseInsensitive(), SymbolSet<FieldVariableSymbol>.Empty, default));
			yield return ("STRING[17]", new StringType(17));
			yield return ("STRING", new StringType(80));
			yield return ("STRING[SIZEOF(MyType)]", new StringType(23));
			yield return ("POINTER TO BYTE", new PointerType(SystemScope.Byte));
			yield return ("POINTER TO POINTER TO SINT", new PointerType(new PointerType(SystemScope.SInt)));
		}
		[Theory]
		[MemberData(nameof(TypeCompiler_Data))]
		public void TypeCompiler(string str, IType expected)
		{
			AssertTypeCompiler(str, bound => Assert.Equal(bound.Code, expected.Code));
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
