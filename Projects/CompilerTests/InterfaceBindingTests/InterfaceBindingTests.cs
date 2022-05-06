using Compiler;
using Compiler.Messages;
using Compiler.Types;
using System.Linq;
using Xunit;

namespace Tests
{
	using static ErrorHelper;

	public sealed class InterfaceBindingTests
	{
		[Fact]
		public void EmptyModule()
		{
			var boundInterface = BindHelper.NewProject.BindInterfaces();

			Assert.Empty(boundInterface.Types);
		}
		[Fact]
		public void EmptyStructure()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("MyType", "STRUCT END_STRUCT")
				.BindInterfaces();

			var myType = Assert.IsType<StructuredTypeSymbol>(Assert.Single(boundInterface.Types));
			Assert.Equal(0, myType.LayoutInfo.Size);
			Assert.Equal("MyType", myType.Name.Original);
			Assert.Empty(myType.Fields);
		}
		[Fact]
		public void Structure_SimpleFields()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("MySimpleType", "STRUCT a : INT; b : REAL; END_STRUCT")
				.BindInterfaces();

			var myType = Assert.IsType<StructuredTypeSymbol>(Assert.Single(boundInterface.Types));
			Assert.Equal(8, myType.LayoutInfo.Size);
			Assert.Equal("MySimpleType", myType.Name.Original);
			Assert.Collection(myType.Fields.OrderBy(f => f.DeclaringSpan.Start),
				a => { Assert.Equal("a", a.Name.Original); Assert.Equal(boundInterface.SystemScope.Int, a.Type); },
				b => { Assert.Equal("b", b.Name.Original); Assert.Equal(boundInterface.SystemScope.Real, b.Type); });
		}

		[Fact]
		public void Union_SimpleFields()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("MySimpleType", "UNION a : INT; b : REAL; END_UNION")
				.BindInterfaces();

			var myType = Assert.IsType<StructuredTypeSymbol>(Assert.Single(boundInterface.Types));
			Assert.Equal(4, myType.LayoutInfo.Size);
			Assert.Equal("MySimpleType", myType.Name.Original);
			Assert.Collection(myType.Fields.OrderBy(f => f.DeclaringSpan.Start),
				a => { Assert.Equal("a", a.Name.Original); Assert.Equal(boundInterface.SystemScope.Int, a.Type); },
				b => { Assert.Equal("b", b.Name.Original); Assert.Equal(boundInterface.SystemScope.Real, b.Type); });
		}
		[Fact]
		public void FieldOfUserdefinedType()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("MyDut", "STRUCT field : REAL; END_STRUCT")
				.AddDut("MySimpleType", "STRUCT field : MyDut; otherField : REAL; END_STRUCT")
				.BindInterfaces();

			var field = Assert.IsType<StructuredTypeSymbol>(boundInterface.Types["MySimpleType"]).Fields["field"];
			Assert.Equal(boundInterface.Types["MyDut"], field.Type);
		}
		[Fact]
		public void FieldOfIncompleteSelf()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("MyDut", "STRUCT field : POINTER TO MyDut; END_STRUCT")
				.BindInterfaces();

			var field = Assert.IsType<StructuredTypeSymbol>(boundInterface.Types["MyDut"]).Fields["field"];
			var baseType = Assert.IsType<PointerType>(field.Type).BaseType;
			Assert.Equal(boundInterface.Types["MyDut"], baseType);
		}

		[Fact]
		public void Error_DuplicateType()
		{
			BindHelper.NewProject
				.AddDut("MySimpleType", "UNION a : INT; b : REAL; END_UNION")
				.AddDut("MySimpleType", "STRUCT xyz : BOOL; END_STRUCT")
				.BindInterfaces(ErrorOfType<SymbolAlreadyExistsMessage>(msg => Assert.Equal("MySimpleType".ToCaseInsensitive(), msg.Name)));
		}

		[Fact]
		public void Error_DuplicateField()
		{
			BindHelper.NewProject
				.AddDut("MySimpleType", "STRUCT field : INT; field : REAL; END_STRUCT")
				.BindInterfaces(ErrorOfType<SymbolAlreadyExistsMessage>(msg => Assert.Equal("field".ToCaseInsensitive(), msg.Name)));
		}
		[Fact]
		public void Error_FieldOfMissingType()
		{
			BindHelper.NewProject
				.AddDut("MySimpleType", "STRUCT field : MissingType; END_STRUCT")
				.BindInterfaces(ErrorOfType<TypeNotFoundMessage>(msg => Assert.Equal("MissingType".ToCaseInsensitive(), msg.Identifier)));
		}
		[Fact]
		public void Error_FieldOfMissingIncompletType()
		{
			BindHelper.NewProject
				.AddDut("MySimpleType", "STRUCT field : POINTER TO MissingType; END_STRUCT")
				.BindInterfaces(ErrorOfType<TypeNotFoundMessage>(msg => Assert.Equal("MissingType".ToCaseInsensitive(), msg.Identifier)));
		}
		[Fact]
		public void Error_FieldOfIncompleteSelf()
		{
			BindHelper.NewProject
				.AddDut("MyDut", "STRUCT field : MyDut; END_STRUCT")
				.BindInterfaces(ErrorOfType<TypeNotCompleteMessage>());
		}

		[Fact]
		public void Error_ArrayOfIncompleteSelf()
		{
			BindHelper.NewProject
				.AddDut("MyDut", "STRUCT field : ARRAY[0..1] OF MyDut2; END_STRUCT")
				.AddDut("MyDut2", "STRUCT field : MyDut; END_STRUCT")
				.BindInterfaces(ErrorOfType<TypeNotCompleteMessage>());
		}
		[Fact]
		public void Error_ArrayOfSizeOfIncompleteSelf()
		{
			BindHelper.NewProject
				.AddDut("MyDut", "STRUCT field : ARRAY[0..SIZEOF(MyDut)] OF INT; END_STRUCT")
				.BindInterfaces(ErrorOfType<TypeNotCompleteMessage>());
		}
		[Fact]
		public void PointerToArrayOfSizeOfIncompleteSelf()
		{
			var boundInterface = BindHelper.NewProject
				.AddDut("MyDut", "STRUCT field : POINTER TO ARRAY[0..SIZEOF(MyDut)] OF INT; END_STRUCT")
				.BindInterfaces();

			var dutType = boundInterface.Types["MyDut"];
			var fieldType = Assert.IsType<StructuredTypeSymbol>(dutType).Fields["field"].Type;
			var arrayUpperBound = Assert.IsType<ArrayType>(Assert.IsType<PointerType>(fieldType).BaseType).Ranges[0].UpperBound;
			Assert.Equal(dutType.LayoutInfo.Size, arrayUpperBound);
			Assert.Equal(4, dutType.LayoutInfo.Size);
		}

		[Theory]
		[InlineData("STRUCT")]
		[InlineData("UNION")]
		public void Error_DutFieldCannotHaveInitalValue(string dutKind)
		{
			BindHelper.NewProject
				.AddDut("DUT", $"{dutKind} field : INT := 0; END_{dutKind}")
				.BindInterfaces(ErrorOfType<VariableCannotHaveInitialValueMessage>());
		}
	}
}
