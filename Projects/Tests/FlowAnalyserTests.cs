#nullable enable
using Xunit;
using Compiler.Messages;
using static Tests.ErrorHelper;
using static Tests.BindHelper;
using System;

namespace Tests
{
	public class FlowAnalyserTests
	{
		private static Action<IMessage> Err_UnassignedOut(string name) =>
			ErrorOfType<VariableMustBeAssignedBeforeEndOfFunctionMessage>(msg => AssertEx.EqualCaseInsensitive(name, msg.Variable.Name));
		private static Action<IMessage> Err_UnassignedVar(string name) =>
			ErrorOfType<UseOfUnassignedVariableMessage>(msg => AssertEx.EqualCaseInsensitive(name, msg.Variable.Name));
		[Fact]
		public void EmptyFunc()
		{
			NewProject
				.AddPou("FUNCTION foo", "")
				.BindBodies()
				.InspectFlowMessages("foo");
		}
		[Fact]
		public void AssignedReturn()
		{
			NewProject
				.AddPou("FUNCTION foo : INT", "foo := 6;")
				.BindBodies()
				.InspectFlowMessages("foo");
		}
		[Fact]
		public void AssignedOutput()
		{
			NewProject
				.AddPou("FUNCTION foo VAR_OUTPUT myOutput : INT; END_VAR", "myOutput := 7;")
				.BindBodies()
				.InspectFlowMessages("foo");
		}
		[Fact]
		public void UnassignedTemp()
		{
			NewProject
				.AddPou("FUNCTION foo VAR myTemp : INT; END_VAR", "")
				.BindBodies()
				.InspectFlowMessages("foo");
		}
		[Fact]
		public void ReadTempBeforeWrite_TempInitialized()
		{
			NewProject
				.AddPou("FUNCTION foo VAR myTemp : INT := 2; myOtherTemp : INT; END_VAR", "myOtherTemp := myTemp;")
				.BindBodies()
				.InspectFlowMessages("foo");
		}
		[Fact]
		public void VarInputAlwaysAssigned()
		{
			NewProject
				.AddPou("FUNCTION foo VAR_INPUT myInput : INT; END_VAR VAR myTemp : INT; END_VAR", "myTemp := myInput;")
				.BindBodies()
				.InspectFlowMessages("foo");
		}

		[Fact]
		public void VarInOutAlwaysAssigned()
		{
			NewProject
				.AddPou("FUNCTION foo VAR_IN_OUT myInout : INT; END_VAR VAR myTemp : INT; END_VAR", "myTemp := myInout;")
				.BindBodies()
				.InspectFlowMessages("foo");
		}
		[Fact]
		public void IF_AssignInBothBranches()
		{
			NewProject
				.AddPou("FUNCTION foo : INT VAR_INPUT xCond : BOOL; END_VAR", "IF xCond THEN foo := 0; ELSE foo := 1; END_IF")
				.BindBodies()
				.InspectFlowMessages("foo");
		}

		[Fact]
		public void Error_UnassignedReturn()
		{
			NewProject
				.AddPou("FUNCTION foo : INT", "")
				.BindBodies()
				.InspectFlowMessages("foo", Err_UnassignedOut("foo"));
		}
		[Fact]
		public void Error_UnassignedOutput()
		{
			NewProject
				.AddPou("FUNCTION foo VAR_OUTPUT myOutput : INT; END_VAR", "")
				.BindBodies()
				.InspectFlowMessages("foo", Err_UnassignedOut("myOutput"));
		}
		[Fact]
		public void Error_ReadTempBeforeWrite()
		{
			NewProject
				.AddPou("FUNCTION foo VAR myTemp : INT; myOtherTemp : INT; END_VAR", "myOtherTemp := myTemp;")
				.BindBodies()
				.InspectFlowMessages("foo", Err_UnassignedVar("myTemp"));
		}
		[Fact]
		public void Error_ReadTempBeforeWrite_NoCascade()
		{
			NewProject
				.AddPou("FUNCTION foo VAR myTemp : INT; myOtherTemp : INT; END_VAR", "myOtherTemp := myTemp; myOtherTemp := myTemp;")
				.BindBodies()
				.InspectFlowMessages("foo", Err_UnassignedVar("myTemp"));
		}
		[Fact]
		public void Error_IF_AssignReturnInSingleBranch()
		{
			NewProject
				.AddPou("FUNCTION foo : INT VAR_INPUT xCond : BOOL; END_VAR", "IF xCond THEN foo := 0; END_IF")
				.BindBodies()
				.InspectFlowMessages("foo", Err_UnassignedOut("foo"));
		}
		[Fact]
		public void Error_IF_AssignReturnInSingleBranch_WithEmptyElse()
		{
			NewProject
				.AddPou("FUNCTION foo : INT VAR_INPUT xCond : BOOL; END_VAR", "IF xCond THEN foo := 0; ELSE END_IF")
				.BindBodies()
				.InspectFlowMessages("foo", Err_UnassignedOut("foo"));
		}
		[Fact]
		public void Error_IF_AssignReturnMissingInELSIF()
		{
			NewProject
				.AddPou("FUNCTION foo : INT VAR_INPUT xCond : BOOL; END_VAR", "IF xCond THEN foo := 0; ELSIF xCond THEN ELSE foo := 1; END_IF")
				.BindBodies()
				.InspectFlowMessages("foo", Err_UnassignedOut("foo"));
		}
		[Fact]
		public void Error_WHILE_AssignInBodyNotEnough()
		{
			NewProject
				.AddPou("FUNCTION foo : INT VAR_INPUT xCond : BOOL; END_VAR", "WHILE xCond DO foo := 0; END_WHILE")
				.BindBodies()
				.InspectFlowMessages("foo", Err_UnassignedOut("foo"));
		}

		[Fact]
		public void Wrn_UnreachableCodeAfterReturn()
		{
			NewProject
				.AddPou("FUNCTION foo VAR_TEMP a : INT; END_VAR", "a := 0; RETURN; a := 1;")
				.BindBodies()
				.InspectFlowMessages("foo", WarningOfType<UnreachableCodeMessage>());
		}

		[Fact]
		public void Wrn_UnreachableCodeAfterReturn_NoErrorForInvalidAccess()
		{
			NewProject
				.AddPou("FUNCTION foo VAR_TEMP a : INT; b : INT; END_VAR", "RETURN; b := a;")
				.BindBodies()
				.InspectFlowMessages("foo", WarningOfType<UnreachableCodeMessage>());
		}

		[Theory]
		[InlineData("EXIT")]
		[InlineData("CONTINUE")]
		public void Wrn_UnreachableCode_CTRL_InLoop(string ctrl)
		{
			NewProject
				.AddPou("FUNCTION foo VAR_INPUT xCond : BOOL; END_VAR VAR a : INT; b : INT; END_VAR", $"WHILE xCond DO a := 0; {ctrl}; a := b; END_WHILE")
				.BindBodies()
				.InspectFlowMessages("foo", WarningOfType<UnreachableCodeMessage>());
		}
		[Theory]
		[InlineData("EXIT")]
		[InlineData("CONTINUE")]
		public void Conditinal_CTRL_InLoop_No_Unreachable(string ctrl)
		{
			NewProject
				.AddPou("FUNCTION foo VAR_INPUT xCond : BOOL; xCond2 : BOOL; END_VAR VAR a : INT; END_VAR", $"WHILE xCond DO IF xCond2 THEN {ctrl}; END_IF a := 0; END_WHILE")
				.BindBodies()
				.InspectFlowMessages("foo");
		}
		[Theory]
		[InlineData("EXIT")]
		[InlineData("CONTINUE")]
		public void UnreachableCode_Wrapped_Conditinal_CTRL_InLoop(string ctrl)
		{
			NewProject
				.AddPou("FUNCTION foo VAR_INPUT xCond : BOOL; xCond2 : BOOL; END_VAR VAR a : INT; END_VAR", $"WHILE xCond DO IF xCond2 THEN {ctrl}; ELSE {ctrl}; END_IF a := 0; END_WHILE")
				.BindBodies()
				.InspectFlowMessages("foo", WarningOfType<UnreachableCodeMessage>());
		}
		[Theory]
		[InlineData("EXIT")]
		[InlineData("CONTINUE")]
		public void Wrn_UnreachableCode_CTRL_InWhileLoop_IgnoredInOuter(string ctrl)
		{
			NewProject
				.AddPou("FUNCTION foo VAR_INPUT xCond : BOOL; END_VAR VAR a : INT; b : INT; c : INT; END_VAR", $"WHILE xCond DO WHILE xCond DO a := 0; {ctrl}; a := b; END_WHILE a := c; END_WHILE")
				.BindBodies()
				.InspectFlowMessages("foo", WarningOfType<UnreachableCodeMessage>(), Err_UnassignedVar("c"));
		}
		[Fact]
		public void CALL_AssignOutput()
		{
			NewProject
				.AddPou("FUNCTION MyFunc VAR_OUTPUT result : INT; END_VAR", "result := 333;")
				.AddPou("FUNCTION foo VAR_OUTPUT myResult : INT; END_VAR", "MyFunc(result => myResult);")
				.BindBodies()
				.InspectFlowMessages("foo");
		}
		[Fact]
		public void Error_CALL_AssignOutput_OnlyAfterCall()
		{
			NewProject
				.AddPou("FUNCTION MyFunc VAR_OUTPUT result : INT; END_VAR VAR_INPUT input : INT; END_VAR", "result := input;")
				.AddPou("FUNCTION foo VAR_OUTPUT myResult : INT; END_VAR", "MyFunc(result => myResult, input := myResult);")
				.BindBodies()
				.InspectFlowMessages("foo", Err_UnassignedVar("myResult"));
		}
		[Fact]
		public void Error_CALL_InOut_MustBeAssigned()
		{
			NewProject
				.AddPou("FUNCTION MyFunc VAR_IN_OUT inout : INT; END_VAR", "")
				.AddPou("FUNCTION foo VAR_TEMP myTemp : INT; END_VAR", "MyFunc(inout := myTemp);")
				.BindBodies()
				.InspectFlowMessages("foo", Err_UnassignedVar("myTemp"));
		}
		[Fact]
		public void CALL_InOut_MustBeAssigned()
		{
			NewProject
				.AddPou("FUNCTION MyFunc VAR_IN_OUT inout : INT; END_VAR", "")
				.AddPou("FUNCTION foo VAR_IN_OUT myTemp : INT; END_VAR", "MyFunc(inout := myTemp);")
				.BindBodies()
				.InspectFlowMessages("foo");
		}
		[Fact]
		public void Error_CALL_FunctionBlock_CalleeMustBeAssigned()
		{
			NewProject
				.AddPou("FUNCTION_BLOCK MyFB", "")
				.AddPou("FUNCTION foo VAR_TEMP myFB : MyFb; END_VAR", "myFb();")
				.BindBodies()
				.InspectFlowMessages("foo", Err_UnassignedVar("myFB"));
		}
		[Fact]
		public void CALL_Function_Nested_CanUseInnerOutput()
		{
			NewProject
				.AddPou("FUNCTION barInner : INT VAR_OUTPUT result : INT; END_VAR", "result := 0; barInner := 1;")
				.AddPou("FUNCTION barOuter VAR_INPUT arg1 : INT; arg2 : INT; END_VAR", "")
				.AddPou("FUNCTION foo VAR_TEMP tmp : INT; END_VAR", "barOuter(arg1 := barInner(result => tmp), arg2 := tmp);")
				.BindBodies()
				.InspectFlowMessages("foo");
		}
		[Fact]
		public void Err_CALL_Function_Nested_CanUseInnerOutput_DependsOnOrder()
		{
			NewProject
				.AddPou("FUNCTION barInner : INT VAR_OUTPUT result : INT; END_VAR", "result := 0; barInner := 1;")
				.AddPou("FUNCTION barOuter VAR_INPUT arg1 : INT; arg2 : INT; END_VAR", "")
				.AddPou("FUNCTION foo VAR_TEMP tmp : INT; END_VAR", "barOuter(arg2 := tmp, arg1 := barInner(result => tmp));")
				.BindBodies()
				.InspectFlowMessages("foo", Err_UnassignedVar("tmp"));
		}
		[Fact]
		public void CALL_FunctionBlock_CalleeMustBeAssigned()
		{
			NewProject
				.AddPou("FUNCTION_BLOCK myFB", "")
				.AddPou("FUNCTION foo VAR_INPUT myFB : MyFB; END_VAR", "myFb();")
				.BindBodies()
				.InspectFlowMessages("foo");
		}
		[Fact]
		public void Err_Assign_ArrayIndex_MustBeReadable()
		{
			NewProject
				.AddPou("FUNCTION foo VAR_TEMP arr : ARRAY[0..1] OF INT; END_VAR", "arr[0] := 7;")
				.BindBodies()
				.InspectFlowMessages("foo", Err_UnassignedVar("arr"));
		}
		[Fact]
		public void Assign_ArrayIndex_MustBeReadable()
		{
			NewProject
				.AddPou("FUNCTION foo VAR_TEMP arr : ARRAY[0..1] OF INT := {[..] := 0}; END_VAR", "arr[0] := 7;")
				.BindBodies()
				.InspectFlowMessages("foo");
		}
		[Fact]
		public void Err_Assign_PointerDeref_MustBeReadable()
		{
			NewProject
				.AddPou("FUNCTION foo VAR_TEMP ptr : POINTER TO INT; END_VAR", "ptr^ := 7;")
				.BindBodies()
				.InspectFlowMessages("foo", Err_UnassignedVar("ptr"));
		}
		[Fact]
		public void Assign_PointerDeref_MustBeReadable()
		{
			NewProject
				.AddPou("FUNCTION foo VAR_INPUT ptr : POINTER TO INT; END_VAR", "ptr^ := 7;")
				.BindBodies()
				.InspectFlowMessages("foo");
		}
		[Fact]
		public void Err_Assign_PointerIndexAccess_MustBeReadable()
		{
			NewProject
				.AddPou("FUNCTION foo VAR_TEMP ptr : POINTER TO INT; END_VAR", "ptr[0] := 7;")
				.BindBodies()
				.InspectFlowMessages("foo", Err_UnassignedVar("ptr"));
		}
		[Fact]
		public void Assign_PointerIndexAccess_MustBeReadable()
		{
			NewProject
				.AddPou("FUNCTION foo VAR_INPUT ptr : POINTER TO INT; END_VAR", "ptr[1] := 7;")
				.BindBodies()
				.InspectFlowMessages("foo");
		}
		[Fact]
		public void Err_Assign_Field_MustBeReadable()
		{
			NewProject
				.AddDutFast("MyDut", "STRUCT x : INT; y : INT; END_STRUCT")
				.AddPou("FUNCTION foo VAR_TEMP myDut : MyDut; END_VAR", "myDut.x := 7;")
				.BindBodies()
				.InspectFlowMessages("foo", Err_UnassignedVar("myDut"));
		}
		[Fact]
		public void Assign_Field_MustBeReadable()
		{
			NewProject
				.AddDutFast("MyDut", "STRUCT x : INT; y : INT; END_STRUCT")
				.AddPou("FUNCTION foo VAR_TEMP myDut : MyDut := {.x := 7, .y := 8}; END_VAR", "myDut.x := 7;")
				.BindBodies()
				.InspectFlowMessages("foo");
		}

		[Fact]
		public void Assign_LeftSideEvaluatedBeforeRightSide()
		{
			NewProject
				.AddPou("FUNCTION Func : INT VAR_OUTPUT result : INT; END_VAR", "Func := 0; result := 7;")
				.AddPou("FUNCTION foo VAR_IN_OUT arr : ARRAY[0..1] OF INT; END_VAR VAR_TEMP tmp : INT; END_VAR", "arr[Func(result => tmp)] := tmp;")
				.BindBodies()
				.InspectFlowMessages("foo");
		}
		[Fact]
		public void Err_CannotUseAssignedValueForRightSide()
		{
			NewProject
				.AddPou("FUNCTION foo VAR_TEMP tmp : INT; END_VAR", "tmp := tmp;")
				.BindBodies()
				.InspectFlowMessages("foo", Err_UnassignedVar("tmp"));
		}

		[Fact]
		public void Err_InitialValue_CannotReadOutput()
		{
			NewProject
				.AddPou("FUNCTION foo VAR_OUTPUT myOutput : INT; END_VAR VAR_TEMP tmp : INT := myOutput; END_VAR", "myOutput := 2;")
				.BindBodies()
				.InspectFlowMessages("foo", Err_UnassignedVar("myOutput"));
		}
		[Fact]
		public void InitialValue_CanReadInput()
		{
			NewProject
				.AddPou("FUNCTION foo VAR_INPUT myInput : INT; END_VAR VAR_TEMP tmp : INT := myInput; END_VAR", "")
				.BindBodies()
				.InspectFlowMessages("foo");
		}
		[Fact]
		public void InitialValue_CanReadInout()
		{
			NewProject
				.AddPou("FUNCTION foo VAR_IN_OUT myInOut : INT; END_VAR VAR_TEMP tmp : INT := myInOut; END_VAR", "")
				.BindBodies()
				.InspectFlowMessages("foo");
		}
		[Fact]
		public void InitialValue_AssignOutputInInitial()
		{
			NewProject
				.AddPou("FUNCTION MyFunc : INT VAR_OUTPUT result : INT; END_VAR", "result := 666; MyFunc := 777;")
				.AddPou("FUNCTION foo VAR_OUTPUT myOutput : INT; END_VAR VAR_TEMP tmp : INT := MyFunc(result => myOutput); END_VAR", "")
				.BindBodies()
				.InspectFlowMessages("foo");
		}
		[Fact]
		public void InitialValue_AssignOutputInInitial_CanUseOutputAfterAssign()
		{
			NewProject
				.AddPou("FUNCTION MyFunc : INT VAR_OUTPUT result : INT; END_VAR", "result := 666; MyFunc := 777;")
				.AddPou("FUNCTION foo VAR_OUTPUT myOutput : INT; END_VAR VAR_TEMP tmp : INT := MyFunc(result => myOutput); tmp2 : INT := myOutput; END_VAR", "")
				.BindBodies()
				.InspectFlowMessages("foo");
		}
		[Fact]
		public void Err_ForStatement_UpperBoundMustBeAssigned()
		{
			NewProject
				.AddPou("FUNCTION foo VAR_TEMP idx : INT; upper : INT; END_VAR", "FOR idx := 0 TO upper DO END_FOR")
				.BindBodies()
				.InspectFlowMessages("foo", Err_UnassignedVar("upper"));
		}
		[Fact]
		public void ForStatement_UpperBoundMustBeAssigned()
		{
			NewProject
				.AddPou("FUNCTION foo VAR_TEMP idx : INT; upper : INT := 10; END_VAR", "FOR idx := 0 TO upper DO END_FOR")
				.BindBodies()
				.InspectFlowMessages("foo");
		}
		[Fact]
		public void Err_ForStatement_InitialMustBeAssigned()
		{
			NewProject
				.AddPou("FUNCTION foo VAR_TEMP idx : INT; initial : INT; END_VAR", "FOR idx := initial TO 10 DO END_FOR")
				.BindBodies()
				.InspectFlowMessages("foo", Err_UnassignedVar("initial"));
		}
		[Fact]
		public void ForStatement_InitialMustBeAssigned()
		{
			NewProject
				.AddPou("FUNCTION foo VAR_TEMP idx : INT; initial : INT := 10; END_VAR", "FOR idx := initial TO 10 DO END_FOR")
				.BindBodies()
				.InspectFlowMessages("foo");
		}
		[Fact]
		public void Err_ForStatement_StepMustBeAssigned()
		{
			NewProject
				.AddPou("FUNCTION foo VAR_TEMP idx : INT; step : INT; END_VAR", "FOR idx := 0 TO 10 BY step DO END_FOR")
				.BindBodies()
				.InspectFlowMessages("foo", Err_UnassignedVar("step"));
		}
		[Fact]
		public void ForStatement_StepMustBeAssigned()
		{
			NewProject
				.AddPou("FUNCTION foo VAR_TEMP idx : INT; step : INT := 10; END_VAR", "FOR idx := 0 TO 10 BY step DO END_FOR")
				.BindBodies()
				.InspectFlowMessages("foo");
		}

		[Fact]
		public void ForStatement_IndexWillBeAssigned()
		{
			NewProject
				.AddPou("FUNCTION foo VAR_TEMP idx : INT; x : INT; END_VAR", "FOR idx := 0 TO 10 DO x := idx; END_FOR x := idx;")
				.BindBodies()
				.InspectFlowMessages("foo");
		}
		[Fact]
		public void ForStatement_IndexWillBeAssigned_BeforeUpperBound()
		{
			NewProject
				.AddPou("FUNCTION foo VAR_TEMP idx : INT; x : INT; END_VAR", "FOR idx := 0 TO idx DO END_FOR")
				.BindBodies()
				.InspectFlowMessages("foo");
		}
		[Fact]
		public void ForStatement_IndexWillBeAssigned_BeforeStep()
		{
			NewProject
				.AddPou("FUNCTION foo VAR_TEMP idx : INT; x : INT; END_VAR", "FOR idx := 0 TO 10 BY idx DO END_FOR")
				.BindBodies()
				.InspectFlowMessages("foo");
		}
		[Fact]
		public void Read_SizeOf()
		{
			NewProject
				.AddPou("FUNCTION foo VAR_TEMP idx : INT; END_VAR", "idx := SIZEOF(INT);")
				.BindBodies()
				.InspectFlowMessages("foo");
		}
		[Fact]
		public void Read_BinaryOperation()
		{
			NewProject
				.AddPou("FUNCTION foo : INT VAR_TEMP a : INT; b : INT; END_VAR", "foo := a + b;")
				.BindBodies()
				.InspectFlowMessages("foo", Err_UnassignedVar("a"), Err_UnassignedVar("b"));
		}
		[Fact]
		public void Read_UnaryOp()
		{
			NewProject
				.AddPou("FUNCTION foo : INT VAR_TEMP a : INT; END_VAR", "foo := -a;")
				.BindBodies()
				.InspectFlowMessages("foo", Err_UnassignedVar("a"));
		}
		[Fact]
		public void Read_Deref()
		{
			NewProject
				.AddPou("FUNCTION foo : INT VAR_TEMP ptr : POINTER TO INT; END_VAR", "foo := ptr^;")
				.BindBodies()
				.InspectFlowMessages("foo", Err_UnassignedVar("ptr"));
		}
	}
}
