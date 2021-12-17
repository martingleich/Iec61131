using Compiler;
using Compiler.Messages;
using Xunit;

namespace Tests.ExpressionBinderTests
{
	using static ErrorTestHelper;

	public static class ExpressionBinderTests_CompoAccess
	{
		[Fact]
		public static void Error_NonStructuredType_Int()
		{
			BindHelper.NewProject
				.WithGlobalVar("value", "INT")
				.BindGlobalExpression("value.xyz", null, ErrorOfType<FieldNotFoundMessage>());
		}
		[Fact]
		public static void Error_StructuredType_DoesNotContainField()
		{
			BindHelper.NewProject
				.AddDut("TYPE myDut : STRUCT myField : USINT; END_STRUCT; END_TYPE")
				.WithGlobalVar("value", "myDut")
				.BindGlobalExpression("value.abc", null, ErrorOfType<FieldNotFoundMessage>());
		}
		[Fact]
		public static void Error_NoCascadingError()
		{
			BindHelper.NewProject
				.BindGlobalExpression("(TRUE * FALSE).abc", null, ErrorOfType<CannotPerformArithmeticOnTypesMessage>());
		}
		[Fact]
		public static void FieldOnVariable()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("TYPE myDut : STRUCT myField : USINT; END_STRUCT; END_TYPE")
				.WithGlobalVar("value", "myDut")
				.BindGlobalExpression("value.myField", null);
			var fieldAccess = Assert.IsType<FieldAccessBoundExpression>(boundExpression);
			Assert.Equal("myField".ToCaseInsensitive(), fieldAccess.Field.Name);
		}
		[Fact]
		public static void FieldOnIndex()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("TYPE myDut : STRUCT myField : USINT; END_STRUCT; END_TYPE")
				.WithGlobalVar("values", "ARRAY[0..10] OF myDut")
				.BindGlobalExpression("values[1].myField", null);
			var fieldAccess = Assert.IsType<FieldAccessBoundExpression>(boundExpression);
			Assert.Equal("myField".ToCaseInsensitive(), fieldAccess.Field.Name);
		}
		[Fact]
		public static void FieldCastedResult()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("TYPE myDut : STRUCT myField : USINT; END_STRUCT; END_TYPE")
				.WithGlobalVar("value", "myDut")
				.BindGlobalExpression("value.myField", "DINT");
			Assert.IsType<ImplicitCastBoundExpression>(boundExpression);
		}

		[Fact]
		public static void Error_TypeNoStatic()
		{
			BindHelper.NewProject
				.AddDut("TYPE myDut : STRUCT myField : USINT; END_STRUCT; END_TYPE")
				.BindGlobalExpression("myDut.myField", null, ErrorOfType<VariableNotFoundMessage>());
		}
		[Fact]
		public static void EnumTypeValue()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("TYPE myEnum : (elem1, elem2); END_TYPE")
				.BindGlobalExpression<VariableBoundExpression>("myEnum::elem2", null);
			var value = Assert.IsType<EnumVariableSymbol>(boundExpression.Variable);
			var innerValue = Assert.IsType<IntLiteralValue>(value.Value.InnerValue);
			Assert.Equal(1, innerValue.Value);
		}
		[Fact]
		public static void EnumTypeValue_ViaAlias()
		{
			var boundExpression = BindHelper.NewProject
				.AddDut("TYPE myEnum : (elem1, elem2); END_TYPE")
				.AddDut("TYPE myAlias : myEnum; END_TYPE")
				.BindGlobalExpression<ImplicitAliasFromBaseTypeCastBoundExpression>("myAlias::elem2", null);
			var boundVariable = Assert.IsType<VariableBoundExpression>(boundExpression.Value);
			var enumVariable = Assert.IsType<EnumVariableSymbol>(boundVariable.Variable);
			var innerValue = Assert.IsType<IntLiteralValue>(enumVariable.Value.InnerValue);
			Assert.Equal(1, innerValue.Value);
		}
		[Fact]
		public static void Error_EnumTypeValue_Missing()
		{
			BindHelper.NewProject
				.AddDut("TYPE myEnum : (elem1, elem2); END_TYPE")
				.BindGlobalExpression("myEnum::elem3", null, ErrorOfType<EnumValueNotFoundMessage>());
		}
		[Fact]
		public static void GvlVariable()
		{
			var boundExpression = BindHelper.NewProject
				.AddGVL("MyGVL", "VAR_GLOBAL gVar : INT; END_VAR")
				.BindGlobalExpression("MyGVL::gVar", null);
			var staticVarExpr = Assert.IsType<VariableBoundExpression>(boundExpression);
			Assert.Equal("gVar".ToCaseInsensitive(), staticVarExpr.Variable.Name);
		}
		[Fact]
		public static void GvlVariable_Cast()
		{
			var boundExpression = BindHelper.NewProject
				.AddGVL("MyGVL", "VAR_GLOBAL gVar : INT; END_VAR")
				.BindGlobalExpression("MyGVL::gVar", "DINT");
			Assert.IsType<ImplicitCastBoundExpression>(boundExpression);
		}
		[Fact]
		public static void Error_GvlVariable_Missing()
		{
			BindHelper.NewProject
				.AddGVL("MyGVL", "VAR_GLOBAL gVar : INT; END_VAR")
				.BindGlobalExpression("MyGVL::abc", null, ErrorOfType<VariableNotFoundMessage>());
		}
		[Fact]
		public static void VariableBeforeGvl()
		{
			var boundExpression = BindHelper.NewProject
				.AddGVL("MyGVL", "VAR_GLOBAL myVar : INT; END_VAR")
				.AddDut("TYPE myDut : STRUCT myVar : INT; END_STRUCT; END_TYPE")
				.WithGlobalVar("MyGvl", "myDut")
				.BindGlobalExpression("MyGVL::myVar", null);
			Assert.IsType<VariableBoundExpression>(boundExpression);
		}
		[Fact]
		public static void Error_UnknownLeftSide()
		{
			BindHelper.NewProject
				.BindGlobalExpression("myThing.myVar", null, ErrorOfType<VariableNotFoundMessage>());
		}
		[Fact]
		public static void DutInGvlAccess()
		{
			var boundExpression = BindHelper.NewProject
				.AddGVL("MyGVL", "VAR_GLOBAL myVar : MyDut; END_VAR")
				.AddDut("TYPE MyDut : STRUCT myField : INT; END_STRUCT; END_TYPE")
				.BindGlobalExpression("MyGVL::myVar.myField", null);
			var fieldAccess = Assert.IsType<FieldAccessBoundExpression>(boundExpression);
			Assert.IsType<VariableBoundExpression>(fieldAccess.BaseExpression);
		}
	}

}
