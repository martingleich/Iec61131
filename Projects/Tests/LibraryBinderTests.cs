using Compiler;
using Compiler.Messages;
using Compiler.Types;
using Xunit;

namespace Tests
{
	using static ErrorTestHelper;

	public static class LibraryBinderTests
	{
		[Fact]
		public static void CallLibraryFunction()
		{
			var boundLibrary = BindHelper.NewNamedProject("MyLib")
				.AddPou("FUNCTION foo VAR_INPUT arg : INT; END_VAR", "")
				.BindInterfaces();
			var boundCall = BindHelper.NewProject
				.AddLibrary(boundLibrary)
				.BindGlobalExpression<CallBoundExpression>("MyLib::foo(7)", null);
			var callee = Assert.IsType<VariableBoundExpression>(boundCall.Callee);
			var func = Assert.IsType<FunctionVariableSymbol>(callee.Variable);
			Assert.Equal(new UniqueSymbolId("MyLib".ToCaseInsensitive(), "foo".ToCaseInsensitive()), func.UniqueId);
		}
		[Fact]
		public static void ReadVariableFromLibrary()
		{
			var boundLibrary = BindHelper.NewNamedProject("MyLib")
				.AddGVL("MyGvl", "VAR_GLOBAL value : INT; END_VAR")
				.BindInterfaces();
			var boundVariable = BindHelper.NewProject
				.AddLibrary(boundLibrary)
				.BindGlobalExpression<VariableBoundExpression>("MYLIB::MYGVL::VALUE", null);
			var variable = Assert.IsType<GlobalVariableSymbol>(boundVariable.Variable);
			Assert.Equal("MyLib::MyGvl::value", variable.UniqueName);
		}

		[Fact]
		public static void TypeFromLibrary()
		{
			var boundLibrary = BindHelper.NewNamedProject("MyLib")
				.AddDut("TYPE MyDut : STRUCT field : INT; END_STRUCT; END_TYPE")
				.BindInterfaces();
			var boundInterfaces = BindHelper.NewProject
				.AddLibrary(boundLibrary)
				.AddDut("TYPE MyDut : STRUCT otherField : MyLib::MyDut; END_STRUCT; END_TYPE")
				.BindInterfaces();
			var myDutType = Assert.IsType<StructuredTypeSymbol>(boundInterfaces.Types["MyDut"]);
			var fieldType = Assert.IsAssignableFrom<ITypeSymbol>(myDutType.Fields["otherField"].Type);
			Assert.Equal("MyLib::MyDut", fieldType.UniqueId.ToString());
		}
		
		[Fact]
		public static void Error_AccessViaOwnNamespace()
		{
			BindHelper.NewNamedProject("MyApp")
				.AddDut("TYPE MyDut1 : STRUCT field : INT; END_STRUCT; END_TYPE")
				.AddDut("TYPE MyDut2 : STRUCT field : MyApp::MyDut1; END_STRUCT; END_TYPE")
				.BindInterfaces(ErrorOfType<ScopeNotFoundMessage>(msg => Assert.Equal("MyApp".ToCaseInsensitive(), msg.Identifier)));
		}

		[Fact]
		public static void Error_DuplicateLibrary()
		{
			var boundLibrary1 = BindHelper.NewNamedProject("MyLib")
				.BindInterfaces();
			var boundLibrary2 = BindHelper.NewNamedProject("MyLib")
				.BindInterfaces();
			BindHelper.NewProject
				.AddLibrary(boundLibrary1)
				.AddLibrary(boundLibrary2)
				.BindInterfaces(ErrorOfType<SymbolAlreadyExistsMessage>());
		}
		[Fact]
		public static void Error_NoAccessToSubLibrary()
		{
			var subLib = BindHelper.NewNamedProject("SubLib")
				.AddDutFast("MyType", "STRUCT END_STRUCT")
				.BindInterfaces();
			var topLib = BindHelper.NewNamedProject("TopLib")
				.AddLibrary(subLib)
				.BindInterfaces();
			BindHelper.NewProject
				.AddLibrary(topLib)
				.AddDutFast("TesterType", "STRUCT field : TopLib::SubLib::MyType; END_STRUCT") 
				.BindInterfaces(ErrorOfType<ScopeNotFoundMessage>(msg => Assert.Equal("SubLib".ToCaseInsensitive(), msg.Identifier)));
		}
		[Fact]
		public static void Error_MissingFunctionInLibrary()
		{
			var boundLibrary = BindHelper.NewNamedProject("MyLib")
				.BindInterfaces();
			BindHelper.NewProject
				.AddLibrary(boundLibrary)
				.BindGlobalExpression("MyLib::bar()", null, ErrorOfType<VariableNotFoundMessage>(msg => Assert.Equal("bar".ToCaseInsensitive(), msg.Identifier)));
		}
		[Fact]
		public static void Error_FunctionInMissingLibrary()
		{
			BindHelper.NewProject
				.BindGlobalExpression("MyLib::bar()", null, ErrorOfType<ScopeNotFoundMessage>(msg => Assert.Equal("MyLib".ToCaseInsensitive(), msg.Identifier)));
		}
		[Fact]
		public static void Error_MissingTypeFromLibrary()
		{
			var boundLibrary = BindHelper.NewNamedProject("MyLib")
				.BindInterfaces();
			BindHelper.NewProject
				.AddLibrary(boundLibrary)
				.AddDut("TYPE MyDut : STRUCT otherField : MyLib::MyDut; END_STRUCT; END_TYPE")
				.BindInterfaces(ErrorOfType<TypeNotFoundMessage>(msg => Assert.Equal("MyDut".ToCaseInsensitive(), msg.Identifier)));
		}
		[Fact]
		public static void Error_TypeFromMissingLibrary()
		{
			BindHelper.NewProject
				.AddDut("TYPE MyDut : STRUCT otherField : MyLib::MyDut; END_STRUCT; END_TYPE")
				.BindInterfaces(ErrorOfType<ScopeNotFoundMessage>(msg => Assert.Equal("MyLib".ToCaseInsensitive(), msg.Identifier)));
		}
	}
}
