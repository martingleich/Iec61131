using Compiler;
using Compiler.Messages;
using Xunit;

namespace Tests.ExpressionBinderTests
{
	using static ErrorHelper;
	using static BindHelper;

	public class ExpressionBinderTests_Initialization_StructuredType
	{
		[Fact]
		public void EmptyType()
		{
			var boundExpression = NewProject
				.AddDutFast("MyType", "STRUCT END_STRUCT")
				.BindGlobalExpression<InitializerBoundExpression>("{}", "MyType");
			Assert.Empty(boundExpression.Elements);
		}
		[Fact]
		public void Fields()
		{
			var boundExpression = NewProject
				.AddDutFast("MyType", "STRUCT field1 : INT; field2 : BOOL; END_STRUCT")
				.BindGlobalExpression<InitializerBoundExpression>("{.field1 := 7, .field2 := TRUE}", "MyType");
			Assert.Collection(boundExpression.Elements,
				FieldElement("field1", BoundIntLiteral(7)),
				FieldElement("field2", BoundBoolLiteral(true)));
		}
		[Fact]
		public void Fields_Unordered()
		{
			var boundExpression = NewProject
				.AddDutFast("MyType", "STRUCT field1 : INT; field2 : BOOL; END_STRUCT")
				.BindGlobalExpression<InitializerBoundExpression>("{.field2 := FALSE, .field1 := 8}", "MyType");
			Assert.Collection(boundExpression.Elements,
				FieldElement("field2", BoundBoolLiteral(false)),
				FieldElement("field1", BoundIntLiteral(8)));
		}
		[Theory]
		[InlineData("[6]")]
		[InlineData("[..]")]
		public void Error_Unsupported_Element(string elem)
		{
			NewProject
				.AddDutFast("MyType", "STRUCT END_STRUCT")
				.BindGlobalExpression($"{{{elem} := FALSE}}", "MyType", ErrorOfType<TypeDoesNotHaveThisElementMessage>());
		}
		[Fact]
		public void Error_UnknwonField()
		{
			NewProject
				.AddDutFast("MyType", "STRUCT END_STRUCT")
				.BindGlobalExpression("{.unknownField := FALSE}", "MyType", ErrorOfType<FieldNotFoundMessage>());
		}
		[Fact]
		public void Error_CannotUseImplicit()
		{
			NewProject
				.AddDutFast("MyType", "STRUCT END_STRUCT")
				.BindGlobalExpression("{7}", "MyType", ErrorOfType<CannotUseImplicitInitializerForThisTypeMessage>());
		}
		[Fact]
		public void Error_DuplicateField()
		{
			NewProject
				.AddDutFast("MyType", "STRUCT field1 : INT; END_STRUCT")
				.BindGlobalExpression("{.field1 := 1, .field1 := 1}", "MyType", ErrorOfType<DuplicateInitializerElementMessage>());
		}
		[Fact]
		public void Error_MissingField()
		{
			NewProject
				.AddDutFast("MyType", "STRUCT field1 : INT; field2 : BOOL; END_STRUCT")
				.BindGlobalExpression("{.field1 := 1}", "MyType", ErrorOfType<FieldNotInitializedMessage>(msg => AssertEx.EqualCaseInsensitive("field2", msg.Field.Name)));
		}
		[Fact]
		public void FunctionBlock()
		{
			var boundExpression = NewProject
				.AddDutFast("MyType", "STRUCT END_STRUCT")
				.AddPou(@"FUNCTION_BLOCK MyFB
VAR_INPUT input : INT; END_VAR
VAR_OUTPUT output : INT; END_VAR
VAR_IN_OUT inout : INT; END_VAR
VAR field : INT; END_VAR
VAR_TEMP temp : INT; END_VAR", "")
				.BindGlobalExpression<InitializerBoundExpression>("{.field := 7}", "MyFB"); // Only VAR-Elements are expected as input
			Assert.Collection(boundExpression.Elements,
				FieldElement("field", BoundIntLiteral(7)));
		}
	}
}
