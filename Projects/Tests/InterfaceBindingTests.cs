using Compiler;
using Compiler.Messages;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Tests
{
	using static ErrorTestHelper;
	public sealed class InterfaceBindingTests
	{
		[Fact]
		public void EmptyModule()
		{
			var project = Project.Empty;
			var boundModule = project.LazyBoundModule.Value;
			Assert.Empty(boundModule.InterfaceMessages);
			Assert.Empty(boundModule.Interface.DutTypes);
		}
		[Fact]
		public void EmptyStructure()
		{
			var source = ParserTestHelper.ParseTypeDeclaration("TYPE MyType : STRUCT END_STRUCT; END_TYPE");
			var project = Project.Empty.Add(new DutLanguageSource(source));
			var boundModule = project.LazyBoundModule.Value;
			Assert.Empty(boundModule.InterfaceMessages);
			var myType = Assert.IsType<StructuredTypeSymbol>(Assert.Single(boundModule.Interface.DutTypes));
			Assert.Equal(0, myType.LayoutInfo.Size);
			Assert.Equal("MyType", myType.Name.Original);
			Assert.Empty(myType.Fields);
		}
		[Fact]
		public void Structure_SimpleFields()
		{
			var source = ParserTestHelper.ParseTypeDeclaration("TYPE MySimpleType : STRUCT a : INT; b : REAL; END_STRUCT; END_TYPE");
			var project = Project.Empty.Add(new DutLanguageSource(source));
			var boundModule = project.LazyBoundModule.Value;
			Assert.Empty(boundModule.InterfaceMessages);
			var myType = Assert.IsType<StructuredTypeSymbol>(Assert.Single(boundModule.Interface.DutTypes));
			Assert.Equal(8, myType.LayoutInfo.Size);
			Assert.Equal("MySimpleType", myType.Name.Original);
			Assert.Collection(myType.Fields.OrderBy(f => f.DeclaringPosition.Start),
				a => { Assert.Equal("a", a.Name.Original); Assert.Equal(BuiltInTypeSymbol.Int, a.Type); },
				b => { Assert.Equal("b", b.Name.Original); Assert.Equal(BuiltInTypeSymbol.Real, b.Type); });
		}

		[Fact]
		public void Union_SimpleFields()
		{
			var source = ParserTestHelper.ParseTypeDeclaration("TYPE MySimpleType : UNION a : INT; b : REAL; END_UNION; END_TYPE");
			var project = Project.Empty.Add(new DutLanguageSource(source));
			var boundModule = project.LazyBoundModule.Value;
			Assert.Empty(boundModule.InterfaceMessages);
			var myType = Assert.IsType<StructuredTypeSymbol>(Assert.Single(boundModule.Interface.DutTypes));
			Assert.Equal(4, myType.LayoutInfo.Size);
			Assert.Equal("MySimpleType", myType.Name.Original);
			Assert.Collection(myType.Fields.OrderBy(f => f.DeclaringPosition.Start),
				a => { Assert.Equal("a", a.Name.Original); Assert.Equal(BuiltInTypeSymbol.Int, a.Type); },
				b => { Assert.Equal("b", b.Name.Original); Assert.Equal(BuiltInTypeSymbol.Real, b.Type); });
		}
		[Fact]
		public void FieldOfUserdefinedType()
		{
			var source1 = ParserTestHelper.ParseTypeDeclaration("TYPE MyDut : STRUCT field : REAL; END_STRUCT; END_TYPE");
			var source2 = ParserTestHelper.ParseTypeDeclaration("TYPE MySimpleType : STRUCT field : MyDut; otherField : REAL; END_STRUCT; END_TYPE");
			var project = Project.Empty.Add(new DutLanguageSource(source2), new DutLanguageSource(source1));
			var boundModule = project.LazyBoundModule.Value;
			Assert.Empty(boundModule.InterfaceMessages);
			var field = Assert.IsType<StructuredTypeSymbol>(boundModule.Interface.DutTypes["MySimpleType"]).Fields["field"];
			Assert.Equal(boundModule.Interface.DutTypes["MyDut"], field.Type);
		}
		[Fact]
		public void FieldOfIncompleteSelf()
		{
			var source = ParserTestHelper.ParseTypeDeclaration("TYPE MyDut : STRUCT field : POINTER TO MyDut; END_STRUCT; END_TYPE");
			var project = Project.Empty.Add(new DutLanguageSource(source));
			var boundModule = project.LazyBoundModule.Value;
			Assert.Empty(boundModule.InterfaceMessages);
			var field = Assert.IsType<StructuredTypeSymbol>(boundModule.Interface.DutTypes["MyDut"]).Fields["field"];
			var baseType = Assert.IsType<PointerTypeSymbol>(field.Type).BaseType;
			Assert.Equal(boundModule.Interface.DutTypes["MyDut"], baseType);
		}

		[Fact]
		public void Error_DuplicateType()
		{
			var source1 = ParserTestHelper.ParseTypeDeclaration("TYPE MySimpleType : UNION a : INT; b : REAL; END_UNION; END_TYPE");
			var source2 = ParserTestHelper.ParseTypeDeclaration("TYPE MySimpleType : STRUCT xyz : BOOL; END_STRUCT; END_TYPE");
			var project = Project.Empty.Add(new DutLanguageSource(source1), new DutLanguageSource(source2));
			var boundModule = project.LazyBoundModule.Value;
			ExactlyMessages(ErrorOfType<SymbolAlreadyExistsMessage>(msg => Assert.Equal("MySimpleType".ToCaseInsensitive(), msg.Name)))(boundModule.InterfaceMessages);
		}

		[Fact]
		public void Error_DuplicateField()
		{
			var source = ParserTestHelper.ParseTypeDeclaration("TYPE MySimpleType : STRUCT field : INT; field : REAL; END_STRUCT; END_TYPE");
			var project = Project.Empty.Add(new DutLanguageSource(source));
			var boundModule = project.LazyBoundModule.Value;
			ExactlyMessages(ErrorOfType<SymbolAlreadyExistsMessage>(msg => Assert.Equal("field".ToCaseInsensitive(), msg.Name)))(boundModule.InterfaceMessages);
		}
		[Fact]
		public void Error_FieldOfMissingType()
		{
			var source = ParserTestHelper.ParseTypeDeclaration("TYPE MySimpleType : STRUCT field : MissingType; END_STRUCT; END_TYPE");
			var project = Project.Empty.Add(new DutLanguageSource(source));
			var boundModule = project.LazyBoundModule.Value;
			ExactlyMessages(ErrorOfType<TypeNotFoundMessage>(msg => Assert.Equal("MissingType", msg.Identifier)))(boundModule.InterfaceMessages);
		}
		[Fact]
		public void Error_FieldOfMissingIncompletType()
		{
			var source = ParserTestHelper.ParseTypeDeclaration("TYPE MySimpleType : STRUCT field : POINTER TO MissingType; END_STRUCT; END_TYPE");
			var project = Project.Empty.Add(new DutLanguageSource(source));
			var boundModule = project.LazyBoundModule.Value;
			ExactlyMessages(ErrorOfType<TypeNotFoundMessage>(msg => Assert.Equal("MissingType", msg.Identifier)))(boundModule.InterfaceMessages);
		}
		[Fact]
		public void Error_FieldOfIncompleteSelf()
		{
			var source = ParserTestHelper.ParseTypeDeclaration("TYPE MyDut : STRUCT field : MyDut; END_STRUCT; END_TYPE");
			var project = Project.Empty.Add(new DutLanguageSource(source));
			var boundModule = project.LazyBoundModule.Value;
			ExactlyMessages(ErrorOfType<TypeNotCompleteMessage>())(boundModule.InterfaceMessages);
		}
		[Fact]
		public void Error_ArrayOfIncompleteSelf()
		{
			var source1 = ParserTestHelper.ParseTypeDeclaration("TYPE MyDut : STRUCT field : ARRAY[0..1] OF MyDut2; END_STRUCT; END_TYPE");
			var source2 = ParserTestHelper.ParseTypeDeclaration("TYPE MyDut2 : STRUCT field : MyDut; END_STRUCT; END_TYPE");
			var project = Project.Empty.Add(new DutLanguageSource(source1), new DutLanguageSource(source2));
			var boundModule = project.LazyBoundModule.Value;
			ExactlyMessages(ErrorOfType<TypeNotCompleteMessage>())(boundModule.InterfaceMessages);
		}
		[Fact]
		public void Error_ArrayOfSizeOfIncompleteSelf()
		{
			var source1 = ParserTestHelper.ParseTypeDeclaration("TYPE MyDut : STRUCT field : ARRAY[0..SIZEOF(MyDut)] OF INT; END_STRUCT; END_TYPE");
			var project = Project.Empty.Add(new DutLanguageSource(source1));
			var boundModule = project.LazyBoundModule.Value;
			ExactlyMessages(ErrorOfType<TypeNotCompleteMessage>())(boundModule.InterfaceMessages);
		}
		[Fact]
		public void PointerToArrayOfSizeOfIncompleteSelf()
		{
			var source1 = ParserTestHelper.ParseTypeDeclaration("TYPE MyDut : STRUCT field : POINTER TO ARRAY[0..SIZEOF(MyDut)] OF INT; END_STRUCT; END_TYPE");
			var project = Project.Empty.Add(new DutLanguageSource(source1));
			var boundModule = project.LazyBoundModule.Value;
			Assert.Empty(boundModule.InterfaceMessages);
			var dutType = boundModule.Interface.DutTypes["MyDut"];
			var fieldType = Assert.IsType<StructuredTypeSymbol>(dutType).Fields["field"].Type;
			var arrayUpperBound = Assert.IsType<ArrayTypeSymbol>(Assert.IsType<PointerTypeSymbol>(fieldType).BaseType).Ranges[0].UpperBound;
			Assert.Equal(dutType.LayoutInfo.Size, arrayUpperBound);
			Assert.Equal(4, dutType.LayoutInfo.Size);
		}

	}

	public sealed class TypeCompilerTests
	{
		private sealed class NaiveScope : IScope
		{
			private readonly StructuredTypeSymbol MyType = new (default, false, "MyType".ToCaseInsensitive(), SymbolSet<FieldSymbol>.Empty, new LayoutInfo(23, 8));

			public EnumTypeSymbol CurrentEnum => throw new NotImplementedException();

			public ErrorsAnd<ITypeSymbol> LookupType(CaseInsensitiveString identifier, SourcePosition sourcePosition)
			{
				if (identifier == "MyType".ToCaseInsensitive())
					return MyType;
				else
					return ErrorsAnd.Create(ITypeSymbol.CreateError(sourcePosition, identifier), new TypeNotFoundMessage(identifier.Original, sourcePosition));
			}

			public ErrorsAnd<ITypeSymbol> LookupTypeIncomplete(CaseInsensitiveString identifier, SourcePosition sourcePosition)
				=> LookupType(identifier, sourcePosition);

			public ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourcePosition sourcePosition)
			{
				throw new NotImplementedException();
			}
		}
		
		private static void AssertTypeCompiler(string input, Action<IType> check)
		{
			var naiveScope = new NaiveScope();
			var source = ParserTestHelper.ParseType(input);
			var bag = new MessageBag();
			var bound = Compiler.TypeCompiler.MapComplete(naiveScope, source, bag);
			Assert.Empty(bag);
			check(bound);
		}

		public static IEnumerable<object[]> TypeCompiler_Data() => TypeCompiler_Data_Real().Select(v => new object[] { v.Item1, v.Item2 });
		public static IEnumerable<(string, IType)> TypeCompiler_Data_Real()
		{
			yield return ("INT", BuiltInTypeSymbol.Int);
			yield return ("BOOL", BuiltInTypeSymbol.Bool);
			yield return ("ARRAY[0..1] OF LREAL", new ArrayTypeSymbol(BuiltInTypeSymbol.LReal, ImmutableArray.Create(new ArrayRange(0, 1))));
			yield return ("ARRAY[0..1,2..5] OF DATE", new ArrayTypeSymbol(BuiltInTypeSymbol.Date, ImmutableArray.Create(new ArrayRange(0, 1), new ArrayRange(2, 5))));
			yield return ("ARRAY[0..0] OF LREAL", new ArrayTypeSymbol(BuiltInTypeSymbol.LReal, ImmutableArray.Create(new ArrayRange(0, 0))));
			yield return ("ARRAY[0..-1] OF LREAL", new ArrayTypeSymbol(BuiltInTypeSymbol.LReal, ImmutableArray.Create(new ArrayRange(0, -1))));
			yield return ("MyType", new StructuredTypeSymbol(default, false, "MyType".ToCaseInsensitive(), SymbolSet<FieldSymbol>.Empty, default));
			yield return ("STRING[17]", new StringTypeSymbol(17));
			yield return ("STRING", new StringTypeSymbol(80));
			yield return ("STRING[SIZEOF(MyType)]", new StringTypeSymbol(23));
			yield return ("POINTER TO BYTE", new PointerTypeSymbol(BuiltInTypeSymbol.Byte));
			yield return ("POINTER TO POINTER TO SINT", new PointerTypeSymbol(new PointerTypeSymbol(BuiltInTypeSymbol.SInt)));
		}
		[Theory]
		[MemberData(nameof(TypeCompiler_Data))]
		public void TypeCompiler(string str, IType expected)
		{
			AssertTypeCompiler(str, bound => Assert.Equal(bound.Code, expected.Code));
		}
	}

	public sealed class EnumBindingTests
	{
		[Fact]
		public void EmptyEnum()
		{
			var source = ParserTestHelper.ParseTypeDeclaration("TYPE MyEnum : (); END_TYPE");
			var project = Project.Empty.Add(new DutLanguageSource(source));
			var boundModule = project.LazyBoundModule.Value;
			Assert.Empty(boundModule.InterfaceMessages);
			var myEnum = Assert.IsType<EnumTypeSymbol>(boundModule.Interface.DutTypes["MyEnum"]);
			Assert.Empty(myEnum.Values);
			Assert.Equal(BuiltInTypeSymbol.DInt, myEnum.BaseType);
		}
		[Fact]
		public void FullyInitialisedEnum()
		{
			var source = ParserTestHelper.ParseTypeDeclaration("TYPE MyEnum : (First := 1, Second := 2); END_TYPE");
			var project = Project.Empty.Add(new DutLanguageSource(source));
			var boundModule = project.LazyBoundModule.Value;
			Assert.Empty(boundModule.InterfaceMessages);
			var myEnum = Assert.IsType<EnumTypeSymbol>(boundModule.Interface.DutTypes["MyEnum"]);
			Assert.Collection(myEnum.Values.OrderBy(e => e.DeclaringPosition.Start),
				first => { Assert.Equal("First", first.Name.Original); Assert.Equal(1, ((DIntLiteralValue)first.Value.InnerValue).Value); },
				second => { Assert.Equal("Second", second.Name.Original); Assert.Equal(2, ((DIntLiteralValue)second.Value.InnerValue).Value); }
				);
			Assert.Equal(BuiltInTypeSymbol.DInt, myEnum.BaseType);
		}
		[Fact]
		public void AutoInitialisedEnum()
		{
			var source = ParserTestHelper.ParseTypeDeclaration("TYPE MyEnum : (First, Second); END_TYPE");
			var project = Project.Empty.Add(new DutLanguageSource(source));
			var boundModule = project.LazyBoundModule.Value;
			Assert.Empty(boundModule.InterfaceMessages);
			var myEnum = Assert.IsType<EnumTypeSymbol>(boundModule.Interface.DutTypes["MyEnum"]);
			Assert.Collection(myEnum.Values.OrderBy(e => e.DeclaringPosition.Start),
				first => { Assert.Equal("First", first.Name.Original); Assert.Equal(0, ((DIntLiteralValue)first.Value.InnerValue).Value); },
				second => { Assert.Equal("Second", second.Name.Original); Assert.Equal(1, ((DIntLiteralValue)second.Value.InnerValue).Value); }
				);
			Assert.Equal(BuiltInTypeSymbol.DInt, myEnum.BaseType);
		}
		[Fact]
		public void ReferenceOtherEnumValues()
		{
			var source = ParserTestHelper.ParseTypeDeclaration("TYPE MyEnum : (First := 1, Second := First); END_TYPE");
			var project = Project.Empty.Add(new DutLanguageSource(source));
			var boundModule = project.LazyBoundModule.Value;
			Assert.Empty(boundModule.InterfaceMessages);
			var myEnum = Assert.IsType<EnumTypeSymbol>(boundModule.Interface.DutTypes["MyEnum"]);
			Assert.Collection(myEnum.Values.OrderBy(e => e.DeclaringPosition.Start),
				first => { Assert.Equal("First", first.Name.Original); Assert.Equal(1, ((DIntLiteralValue)first.Value.InnerValue).Value); },
				second => { Assert.Equal("Second", second.Name.Original); Assert.Equal(1, ((DIntLiteralValue)second.Value.InnerValue).Value); }
				);
			Assert.Equal(BuiltInTypeSymbol.DInt, myEnum.BaseType);
		}
		[Fact]
		public void Error_RecursiveEnumDeclaration()
		{
			var source = ParserTestHelper.ParseTypeDeclaration("TYPE MyEnum : (First := Second, Second := First); END_TYPE");
			var project = Project.Empty.Add(new DutLanguageSource(source));
			var boundModule = project.LazyBoundModule.Value;
			ExactlyMessages(ErrorOfType<RecursiveConstantDeclarationMessage>())(boundModule.InterfaceMessages);
		}
	}
}
