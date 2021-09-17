using Compiler;
using Compiler.Messages;
using Compiler.Types;
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
		private static readonly SystemScope SystemScope = new ();
		[Fact]
		public void EmptyModule()
		{
			var boundInterface = BindHelper.NewProject.BindInterfaces();

			Assert.Empty(boundInterface.DutTypes);
		}
		[Fact]
		public void EmptyStructure()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("TYPE MyType : STRUCT END_STRUCT; END_TYPE")
				.BindInterfaces();

			var myType = Assert.IsType<StructuredTypeSymbol>(Assert.Single(boundInterface.DutTypes));
			Assert.Equal(0, myType.LayoutInfo.Size);
			Assert.Equal("MyType", myType.Name.Original);
			Assert.Empty(myType.Fields);
		}
		[Fact]
		public void Structure_SimpleFields()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("TYPE MySimpleType : STRUCT a : INT; b : REAL; END_STRUCT; END_TYPE")
				.BindInterfaces();

			var myType = Assert.IsType<StructuredTypeSymbol>(Assert.Single(boundInterface.DutTypes));
			Assert.Equal(8, myType.LayoutInfo.Size);
			Assert.Equal("MySimpleType", myType.Name.Original);
			Assert.Collection(myType.Fields.OrderBy(f => f.DeclaringPosition.Start),
				a => { Assert.Equal("a", a.Name.Original); Assert.Equal(SystemScope.Int, a.Type); },
				b => { Assert.Equal("b", b.Name.Original); Assert.Equal(SystemScope.Real, b.Type); });
		}

		[Fact]
		public void Union_SimpleFields()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("TYPE MySimpleType : UNION a : INT; b : REAL; END_UNION; END_TYPE")
				.BindInterfaces();

			var myType = Assert.IsType<StructuredTypeSymbol>(Assert.Single(boundInterface.DutTypes));
			Assert.Equal(4, myType.LayoutInfo.Size);
			Assert.Equal("MySimpleType", myType.Name.Original);
			Assert.Collection(myType.Fields.OrderBy(f => f.DeclaringPosition.Start),
				a => { Assert.Equal("a", a.Name.Original); Assert.Equal(SystemScope.Int, a.Type); },
				b => { Assert.Equal("b", b.Name.Original); Assert.Equal(SystemScope.Real, b.Type); });
		}
		[Fact]
		public void FieldOfUserdefinedType()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("TYPE MyDut : STRUCT field : REAL; END_STRUCT; END_TYPE")
				.AddDut("TYPE MySimpleType : STRUCT field : MyDut; otherField : REAL; END_STRUCT; END_TYPE")
				.BindInterfaces();

			var field = Assert.IsType<StructuredTypeSymbol>(boundInterface.DutTypes["MySimpleType"]).Fields["field"];
			Assert.Equal(boundInterface.DutTypes["MyDut"], field.Type);
		}
		[Fact]
		public void FieldOfIncompleteSelf()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("TYPE MyDut : STRUCT field : POINTER TO MyDut; END_STRUCT; END_TYPE")
				.BindInterfaces();

			var field = Assert.IsType<StructuredTypeSymbol>(boundInterface.DutTypes["MyDut"]).Fields["field"];
			var baseType = Assert.IsType<PointerType>(field.Type).BaseType;
			Assert.Equal(boundInterface.DutTypes["MyDut"], baseType);
		}

		[Fact]
		public void Error_DuplicateType()
		{
			BindHelper.NewProject
				.AddDut("TYPE MySimpleType : UNION a : INT; b : REAL; END_UNION; END_TYPE")
				.AddDut("TYPE MySimpleType : STRUCT xyz : BOOL; END_STRUCT; END_TYPE")
				.BindInterfaces(ErrorOfType<SymbolAlreadyExistsMessage>(msg => Assert.Equal("MySimpleType".ToCaseInsensitive(), msg.Name)));
		}

		[Fact]
		public void Error_DuplicateField()
		{
			BindHelper.NewProject
				.AddDut("TYPE MySimpleType : STRUCT field : INT; field : REAL; END_STRUCT; END_TYPE")
				.BindInterfaces(ErrorOfType<SymbolAlreadyExistsMessage>(msg => Assert.Equal("field".ToCaseInsensitive(), msg.Name)));
		}
		[Fact]
		public void Error_FieldOfMissingType()
		{
			BindHelper.NewProject
				.AddDut("TYPE MySimpleType : STRUCT field : MissingType; END_STRUCT; END_TYPE")
				.BindInterfaces(ErrorOfType<TypeNotFoundMessage>(msg => Assert.Equal("MissingType", msg.Identifier)));
		}
		[Fact]
		public void Error_FieldOfMissingIncompletType()
		{
			BindHelper.NewProject
				.AddDut("TYPE MySimpleType : STRUCT field : POINTER TO MissingType; END_STRUCT; END_TYPE")
				.BindInterfaces(ErrorOfType<TypeNotFoundMessage>(msg => Assert.Equal("MissingType", msg.Identifier)));
		}
		[Fact]
		public void Error_FieldOfIncompleteSelf()
		{
			BindHelper.NewProject
				.AddDut("TYPE MyDut : STRUCT field : MyDut; END_STRUCT; END_TYPE")
				.BindInterfaces(ErrorOfType<TypeNotCompleteMessage>());
		}

		[Fact]
		public void Error_ArrayOfIncompleteSelf()
		{
			BindHelper.NewProject
				.AddDut("TYPE MyDut : STRUCT field : ARRAY[0..1] OF MyDut2; END_STRUCT; END_TYPE")
				.AddDut("TYPE MyDut2 : STRUCT field : MyDut; END_STRUCT; END_TYPE")
				.BindInterfaces(ErrorOfType<TypeNotCompleteMessage>(), ErrorOfType<TypeNotCompleteMessage>());
		}
		[Fact]
		public void Error_ArrayOfSizeOfIncompleteSelf()
		{
			BindHelper.NewProject
				.AddDut("TYPE MyDut : STRUCT field : ARRAY[0..SIZEOF(MyDut)] OF INT; END_STRUCT; END_TYPE")
				.BindInterfaces(ErrorOfType<TypeNotCompleteMessage>());
		}
		[Fact]
		public void PointerToArrayOfSizeOfIncompleteSelf()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("TYPE MyDut : STRUCT field : POINTER TO ARRAY[0..SIZEOF(MyDut)] OF INT; END_STRUCT; END_TYPE")
				.BindInterfaces();

			var dutType = boundInterface.DutTypes["MyDut"];
			var fieldType = Assert.IsType<StructuredTypeSymbol>(dutType).Fields["field"].Type;
			var arrayUpperBound = Assert.IsType<ArrayType>(Assert.IsType<PointerType>(fieldType).BaseType).Ranges[0].UpperBound;
			Assert.Equal(dutType.LayoutInfo.Size, arrayUpperBound);
			Assert.Equal(4, dutType.LayoutInfo.Size);
		}
	}

	public sealed class TypeCompilerTests
	{
		private static readonly SystemScope SystemScope = new ();
		private sealed class NaiveScope : AInnerScope
		{
			private readonly StructuredTypeSymbol MyType = new (default, false, "MyType".ToCaseInsensitive(), SymbolSet<FieldSymbol>.Empty, new LayoutInfo(23, 8));

			public NaiveScope() : base(RootScope.Instance)
			{
			}

			public override ErrorsAnd<ITypeSymbol> LookupType(CaseInsensitiveString identifier, SourcePosition sourcePosition)
				=> identifier == MyType.Name
					? MyType
					: base.LookupType(identifier, sourcePosition);
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

		public static IEnumerable<object[]> TypeCompiler_Data() => TypeCompilerData_Typed().Select(v => new object[] { v.Item1, v.Item2 });
		public static IEnumerable<(string, IType)> TypeCompilerData_Typed()
		{
			yield return ("INT", SystemScope.Int);
			yield return ("BOOL", SystemScope.Bool);
			yield return ("ARRAY[0..1] OF LREAL", new ArrayType(SystemScope.LReal, ImmutableArray.Create(new ArrayType.Range(0, 1))));
			yield return ("ARRAY[0..1,2..5] OF DATE", new ArrayType(SystemScope.Date, ImmutableArray.Create(new ArrayType.Range(0, 1), new ArrayType.Range(2, 5))));
			yield return ("ARRAY[0..0] OF LREAL", new ArrayType(SystemScope.LReal, ImmutableArray.Create(new ArrayType.Range(0, 0))));
			yield return ("ARRAY[0..-1] OF LREAL", new ArrayType(SystemScope.LReal, ImmutableArray.Create(new ArrayType.Range(0, -1))));
			yield return ("MyType", new StructuredTypeSymbol(default, false, "MyType".ToCaseInsensitive(), SymbolSet<FieldSymbol>.Empty, default));
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
	}

	public sealed class EnumBindingTests
	{
		private static readonly SystemScope SystemScope = new ();
		[Fact]
		public void EmptyEnum()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("TYPE MyEnum : (); END_TYPE")
				.BindInterfaces();

			var myEnum = Assert.IsType<EnumTypeSymbol>(boundInterface.DutTypes["MyEnum"]);
			Assert.Empty(myEnum.Values);
			Assert.Equal(SystemScope.DInt, myEnum.BaseType);
		}

		[Fact]
		public void FullyInitialisedEnum()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("TYPE MyEnum : (First := 1, Second := 2); END_TYPE")
				.BindInterfaces();

			var myEnum = Assert.IsType<EnumTypeSymbol>(boundInterface.DutTypes["MyEnum"]);
			Assert.Collection(myEnum.Values.OrderBy(e => e.DeclaringPosition.Start),
				first => { Assert.Equal("First", first.Name.Original); Assert.Equal(1, Assert.IsType<DIntLiteralValue>(first.Value.InnerValue).Value); },
				second => { Assert.Equal("Second", second.Name.Original); Assert.Equal(2, Assert.IsType<DIntLiteralValue>(second.Value.InnerValue).Value); }
				);
			Assert.Equal(SystemScope.DInt, myEnum.BaseType);
		}

		[Fact]
		public void AutoInitialisedEnum()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("TYPE MyEnum : (First, Second); END_TYPE")
				.BindInterfaces();

			var myEnum = Assert.IsType<EnumTypeSymbol>(boundInterface.DutTypes["MyEnum"]);
			Assert.Collection(myEnum.Values.OrderBy(e => e.DeclaringPosition.Start),
				first => { Assert.Equal("First", first.Name.Original); Assert.Equal(0, Assert.IsType<DIntLiteralValue>(first.Value.InnerValue).Value); },
				second => { Assert.Equal("Second", second.Name.Original); Assert.Equal(1, Assert.IsType<DIntLiteralValue>(second.Value.InnerValue).Value); }
				);
			Assert.Equal(SystemScope.DInt, myEnum.BaseType);
		}

		[Fact]
		public void ReferenceOtherEnumValues()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("TYPE MyEnum : (First := 1, Second := First); END_TYPE")
				.BindInterfaces();

			var myEnum = Assert.IsType<EnumTypeSymbol>(boundInterface.DutTypes["MyEnum"]);
			Assert.Collection(myEnum.Values.OrderBy(e => e.DeclaringPosition.Start),
				first => { Assert.Equal("First", first.Name.Original); Assert.Equal(1, Assert.IsType<DIntLiteralValue>(first.Value.InnerValue).Value); },
				second => { Assert.Equal("Second", second.Name.Original); Assert.Equal(1, Assert.IsType<DIntLiteralValue>(second.Value.InnerValue).Value); }
				);
			Assert.Equal(SystemScope.DInt, myEnum.BaseType);
		}

		[Fact]
		public void Error_RecursiveEnumDeclaration()
		{
			BindHelper.NewProject
				.AddDut("TYPE MyEnum : (First := Second, Second := First); END_TYPE")
				.BindInterfaces(ErrorOfType<RecursiveConstantDeclarationMessage>());
		}
	}

	public sealed class FunctionBindingTests
	{
		private static readonly SystemScope SystemScope = new ();
		[Fact]
		public void EmptyFunction()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("FUNCTION MyFunction", "")
				.BindInterfaces();
			var myFunction = boundInterface.FunctionSymbols["MyFunction"];
			Assert.Empty(myFunction.Parameters);
		}

		[Fact]
		public void Function_WithInput()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("FUNCTION MyFunction VAR_INPUT myInput : INT; END_VAR", "")
				.BindInterfaces();
			var myFunction = boundInterface.FunctionSymbols["MyFunction"];
			Assert.Collection(myFunction.Parameters,
				p => { Assert.Equal(ParameterKind.Input, p.Kind); Assert.Equal("myInput".ToCaseInsensitive(), p.Name); Assert.Equal(SystemScope.Int, p.Type); });
		}

		[Fact]
		public void Function_WithOutput()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("FUNCTION MyFunction VAR_OUTPUT myOutput : BOOL; END_VAR", "")
				.BindInterfaces();
			var myFunction = boundInterface.FunctionSymbols["MyFunction"];
			Assert.Collection(myFunction.Parameters,
				p => { Assert.Equal(ParameterKind.Output, p.Kind); Assert.Equal("myOutput".ToCaseInsensitive(), p.Name); Assert.Equal(SystemScope.Bool, p.Type); });
		}
		[Fact]
		public void Function_WithInOut()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("FUNCTION MyFunction VAR_IN_OUT myInOut : REAL; END_VAR", "")
				.BindInterfaces();
			var myFunction = boundInterface.FunctionSymbols["MyFunction"];
			Assert.Collection(myFunction.Parameters,
				p => { Assert.Equal(ParameterKind.InOut, p.Kind); Assert.Equal("myInOut".ToCaseInsensitive(), p.Name); Assert.Equal(SystemScope.Real, p.Type); });
		}
		[Fact]
		public void Function_TempIsIgnored()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("FUNCTION MyFunction VAR_TEMP myTemp : REAL; END_VAR", "")
				.BindInterfaces();
			var myFunction = boundInterface.FunctionSymbols["MyFunction"];
			Assert.Empty(myFunction.Parameters);
		}
		[Fact]
		public void Function_VarIsIgnored()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("FUNCTION MyFunction VAR myTemp : REAL; END_VAR", "")
				.BindInterfaces();
			var myFunction = boundInterface.FunctionSymbols["MyFunction"];
			Assert.Empty(myFunction.Parameters);
		}
		[Fact]
		public void Function_InputsInSameBlock()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("FUNCTION MyFunction VAR_INPUT input1 : REAL; input2 : INT; END_VAR", "")
				.BindInterfaces();
			var myFunction = boundInterface.FunctionSymbols["MyFunction"];
			Assert.Collection(myFunction.Parameters,
				p => Assert.Equal("input1".ToCaseInsensitive(), p.Name),
				p => Assert.Equal("input2".ToCaseInsensitive(), p.Name));
		}
		[Fact]
		public void Function_InputsInDiffrentBlock()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("FUNCTION MyFunction VAR_INPUT input1 : REAL; END_VAR VAR_INPUT input2 : INT; END_VAR", "")
				.BindInterfaces();
			var myFunction = boundInterface.FunctionSymbols["MyFunction"];
			Assert.Collection(myFunction.Parameters,
				p => Assert.Equal("input1".ToCaseInsensitive(), p.Name),
				p => Assert.Equal("input2".ToCaseInsensitive(), p.Name));
		}
		[Fact]
		public void Function_ReturnAsOutput()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("FUNCTION MyFunction : REAL VAR_OUTPUT firstOutput : BOOL; END_VAR", "")
				.BindInterfaces();
			var myFunction = boundInterface.FunctionSymbols["MyFunction"];
			Assert.Collection(myFunction.Parameters,
				p => { Assert.Equal(ParameterKind.Output, p.Kind); Assert.Equal("firstOutput".ToCaseInsensitive(), p.Name); },
				p => { Assert.Equal(ParameterKind.Output, p.Kind); Assert.Equal("MyFunction".ToCaseInsensitive(), p.Name);  Assert.Equal(SystemScope.Real, p.Type); });
		}
		[Fact]
		public void Function_ExplicitReturnOutput()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("FUNCTION MyFunction VAR_OUTPUT MyFunction : BOOL; END_VAR", "")
				.BindInterfaces();
			var myFunction = boundInterface.FunctionSymbols["MyFunction"];
			Assert.Collection(myFunction.Parameters,
				p => { Assert.Equal(ParameterKind.Output, p.Kind); Assert.Equal("MyFunction".ToCaseInsensitive(), p.Name);  Assert.Equal(SystemScope.Bool, p.Type); });
		}
		[Fact]
		public void Function_ComplexTypeArg()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("TYPE MyEnum : (First := 1, Second := First); END_TYPE")
				.AddPou("FUNCTION MyFunction VAR_OUTPUT MyFunction : MyEnum; END_VAR", "")
				.BindInterfaces();
			var myEnum = boundInterface.DutTypes["MyEnum"];
			var myFunction = boundInterface.FunctionSymbols["MyFunction"];
			Assert.Collection(myFunction.Parameters,
				p => { Assert.Equal(ParameterKind.Output, p.Kind); Assert.Equal("MyFunction".ToCaseInsensitive(), p.Name); Assert.Equal(myEnum.Code, p.Type.Code); });
		}
		[Fact]
		public void Function_Error_DuplicateFunction()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("FUNCTION MyFunction", "")
				.AddPou("FUNCTION MyFunction", "")
				.BindInterfaces(ErrorOfType<SymbolAlreadyExistsMessage>(err => Assert.Equal("MyFunction", err.Name.Original)));
		}
		[Fact]
		public void Function_Error_DuplicateArg_SameKind()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("FUNCTION MyFunction VAR_INPUT a : INT; a : INT; END_VAR", "")
				.BindInterfaces(ErrorOfType<SymbolAlreadyExistsMessage>(err => Assert.Equal("a", err.Name.Original)));
		}
		[Fact]
		public void Function_Error_DuplicateArg_DiffrentKind()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("FUNCTION MyFunction VAR_OUTPUT a : INT; END_VAR VAR_INPUT a : INT; END_VAR", "")
				.BindInterfaces(ErrorOfType<SymbolAlreadyExistsMessage>(err => Assert.Equal("a", err.Name.Original)));
		}
		[Fact]
		public void Function_Error_Duplicate_ImplicitReturnVariable()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("FUNCTION MyFunction : REAL VAR_OUTPUT MyFunction : REAL; END_VAR", "")
				.BindInterfaces(ErrorOfType<SymbolAlreadyExistsMessage>(err => Assert.Equal("MyFunction", err.Name.Original)));
		}
	}


	public sealed class ProgramBindingTests
	{
		private static readonly SystemScope SystemScope = new ();
		[Fact]
		public void Empty()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("PROGRAM MyProgram", "")
				.BindInterfaces();
			var myProgram = boundInterface.FunctionSymbols["MyProgram"];
			Assert.Empty(myProgram.Parameters);
		}

		[Fact]
		public void WithInput()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("PROGRAM MyProgram VAR_INPUT myInput : INT; END_VAR", "")
				.BindInterfaces();
			var myProgram = boundInterface.FunctionSymbols["MyProgram"];
			Assert.Collection(myProgram.Parameters,
				p => { Assert.Equal(ParameterKind.Input, p.Kind); Assert.Equal("myInput".ToCaseInsensitive(), p.Name); Assert.Equal(SystemScope.Int, p.Type); });
		}

		[Fact]
		public void WithOutput()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("PROGRAM MyProgram VAR_OUTPUT myOutput : BOOL; END_VAR", "")
				.BindInterfaces();
			var myProgram = boundInterface.FunctionSymbols["MyProgram"];
			Assert.Collection(myProgram.Parameters,
				p => { Assert.Equal(ParameterKind.Output, p.Kind); Assert.Equal("myOutput".ToCaseInsensitive(), p.Name); Assert.Equal(SystemScope.Bool, p.Type); });
		}
		[Fact]
		public void WithInOut()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("PROGRAM MyProgram VAR_IN_OUT myInOut : REAL; END_VAR", "")
				.BindInterfaces();
			var myProgram = boundInterface.FunctionSymbols["MyProgram"];
			Assert.Collection(myProgram.Parameters,
				p => { Assert.Equal(ParameterKind.InOut, p.Kind); Assert.Equal("myInOut".ToCaseInsensitive(), p.Name); Assert.Equal(SystemScope.Real, p.Type); });
		}
		[Fact]
		public void TempIsIgnored()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("PROGRAM MyProgram VAR_TEMP myTemp : REAL; END_VAR", "")
				.BindInterfaces();
			var myProgram = boundInterface.FunctionSymbols["MyProgram"];
			Assert.Empty(myProgram.Parameters);
		}
		[Fact]
		public void VarIsIgnored()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("PROGRAM MyProgram VAR myStatic : REAL; END_VAR", "")
				.BindInterfaces();
			var myProgram = boundInterface.FunctionSymbols["MyProgram"];
			Assert.Empty(myProgram.Parameters);
		}
		[Fact]
		public void InputsInSameBlock()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("PROGRAM MyProgram VAR_INPUT input1 : REAL; input2 : INT; END_VAR", "")
				.BindInterfaces();
			var myProgram = boundInterface.FunctionSymbols["MyProgram"];
			Assert.Collection(myProgram.Parameters,
				p => Assert.Equal("input1".ToCaseInsensitive(), p.Name),
				p => Assert.Equal("input2".ToCaseInsensitive(), p.Name));
		}
		[Fact]
		public void InputsInDiffrentBlock()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("PROGRAM MyProgram VAR_INPUT input1 : REAL; END_VAR VAR_INPUT input2 : INT; END_VAR", "")
				.BindInterfaces();
			var myProgram = boundInterface.FunctionSymbols["MyProgram"];
			Assert.Collection(myProgram.Parameters,
				p => Assert.Equal("input1".ToCaseInsensitive(), p.Name),
				p => Assert.Equal("input2".ToCaseInsensitive(), p.Name));
		}
		[Fact]
		public void ReturnAsOutput()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("PROGRAM MyProgram : REAL VAR_OUTPUT firstOutput : BOOL; END_VAR", "")
				.BindInterfaces();
			var myProgram = boundInterface.FunctionSymbols["MyProgram"];
			Assert.Collection(myProgram.Parameters,
				p => { Assert.Equal(ParameterKind.Output, p.Kind); Assert.Equal("firstOutput".ToCaseInsensitive(), p.Name); },
				p => { Assert.Equal(ParameterKind.Output, p.Kind); Assert.Equal("MyProgram".ToCaseInsensitive(), p.Name);  Assert.Equal(SystemScope.Real, p.Type); });
		}
		[Fact]
		public void ExplicitReturnOutput()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("PROGRAM MyProgram VAR_OUTPUT MyProgram : BOOL; END_VAR", "")
				.BindInterfaces();
			var myProgram = boundInterface.FunctionSymbols["MyProgram"];
			Assert.Collection(myProgram.Parameters,
				p => { Assert.Equal(ParameterKind.Output, p.Kind); Assert.Equal("MyProgram".ToCaseInsensitive(), p.Name);  Assert.Equal(SystemScope.Bool, p.Type); });
		}
		[Fact]
		public void ComplexTypeArg()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("TYPE MyEnum : (First := 1, Second := First); END_TYPE")
				.AddPou("PROGRAM MyProgram VAR_OUTPUT MyProgram : MyEnum; END_VAR", "")
				.BindInterfaces();
			var myEnum = boundInterface.DutTypes["MyEnum"];
			var myProgram = boundInterface.FunctionSymbols["MyProgram"];
			Assert.Collection(myProgram.Parameters,
				p => { Assert.Equal(ParameterKind.Output, p.Kind); Assert.Equal("MyProgram".ToCaseInsensitive(), p.Name); Assert.Equal(myEnum.Code, p.Type.Code); });
		}
		[Fact]
		public void Error_Duplicate()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("PROGRAM MyProgram", "")
				.AddPou("PROGRAM MyProgram", "")
				.BindInterfaces(ErrorOfType<SymbolAlreadyExistsMessage>(err => Assert.Equal("MyProgram", err.Name.Original)));
		}
		[Fact]
		public void Error_DuplicateArg_SameKind()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("PROGRAM MyProgram VAR_INPUT a : INT; a : INT; END_VAR", "")
				.BindInterfaces(ErrorOfType<SymbolAlreadyExistsMessage>(err => Assert.Equal("a", err.Name.Original)));
		}
		[Fact]
		public void Error_DuplicateArg_DiffrentKind()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("PROGRAM MyProgram VAR_OUTPUT a : INT; END_VAR VAR_INPUT a : INT; END_VAR", "")
				.BindInterfaces(ErrorOfType<SymbolAlreadyExistsMessage>(err => Assert.Equal("a", err.Name.Original)));
		}
		[Fact]
		public void Error_Duplicate_ImplicitReturnVariable()
		{
			var boundInterface = BindHelper.NewProject
				.AddPou("PROGRAM MyProgram : REAL VAR_OUTPUT MyProgram : REAL; END_VAR", "")
				.BindInterfaces(ErrorOfType<SymbolAlreadyExistsMessage>(err => Assert.Equal("MyProgram", err.Name.Original)));
		}
	}
}
