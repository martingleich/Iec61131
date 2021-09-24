namespace Tests
{
	using Compiler;
	using Compiler.Messages;
	using System;
	using Xunit;
	using static ErrorTestHelper;
	public sealed class FunctionBodyBindingTests
	{
		[Fact]
		public void BindEmptyBody()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo", "")
				.BindBodies()
				.Inspect("foo", st =>
				{
					var seq = Assert.IsType<SequenceBoundStatement>(st);
					Assert.Empty(seq.Statements);
				});
		}
		[Fact]
		public void BindEmptyStatement()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo", ";")
				.BindBodies()
				.Inspect("foo", st =>
				{
					var seq = Assert.IsType<SequenceBoundStatement>(st);
					var st2 = Assert.Single(seq.Statements);
					var seq2 = Assert.IsType<SequenceBoundStatement>(st2);
					Assert.Empty(seq2.Statements);
				});
		}
		[Theory]
		[InlineData("5", typeof(LiteralBoundExpression))]
		[InlineData("x", typeof(VariableBoundExpression))]
		[InlineData("x + 1", typeof(BinaryOperatorBoundExpression))]
		[InlineData("SIZEOF(INT)", typeof(SizeOfTypeBoundExpression))]
		public void BindExpressionStatement(string expr, Type exprType)
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo VAR x : INT; END_VAR", expr + ";")
				.BindBodies()
				.Inspect("foo", st =>
				{
					var seqSt = Assert.IsType<SequenceBoundStatement>(st);
					var exprSt = Assert.IsType<ExpressionBoundStatement>(Assert.Single(seqSt.Statements));
					Assert.IsType(exprType, exprSt.Expression);
				});
		}

		[Fact]
		public void Error_OnDuplicateVarName()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo VAR x : INT; x : REAL; END_VAR", "")
				.BindBodies(ErrorOfType<SymbolAlreadyExistsMessage>(msg => Assert.Equal("x".ToCaseInsensitive(), msg.Name)));
		}
		[Fact]
		public void Error_OnDuplicateVarNameAndArgument()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo VAR_INPUT x : INT; END_VAR VAR x : REAL; END_VAR", "")
				.BindBodies(ErrorOfType<SymbolAlreadyExistsMessage>(msg => Assert.Equal("x".ToCaseInsensitive(), msg.Name)));
		}
		[Fact]
		public void Namelookup_Argument()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo VAR_INPUT x : INT; END_VAR", "x;")
				.BindBodies()
				.Inspect("foo", st =>
				{
					var exprSt = Assert.IsType<ExpressionBoundStatement>(Assert.Single(Assert.IsType<SequenceBoundStatement>(st).Statements));
					var expr = Assert.IsType<VariableBoundExpression>(exprSt.Expression);
					Assert.Equal("x".ToCaseInsensitive(), expr.Variable.Name);
				});
		}
		[Fact]
		public void Namelookup_Local()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo VAR y : INT; END_VAR", "y;")
				.BindBodies()
				.Inspect("foo", st =>
				{
					var exprSt = Assert.IsType<ExpressionBoundStatement>(Assert.Single(Assert.IsType<SequenceBoundStatement>(st).Statements));
					var expr = Assert.IsType<VariableBoundExpression>(exprSt.Expression);
					Assert.Equal("y".ToCaseInsensitive(), expr.Variable.Name);
				});
		}
		[Fact]
		public void Error_Namelookup_NoExist()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo", "doesNotExist;")
				.BindBodies(ErrorOfType<VariableNotFoundMessage>(msg => Assert.Equal("doesNotExist".ToCaseInsensitive(), msg.Identifier)));
		}
		[Fact]
		public void AssignStatement()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo VAR x : INT; END_VAR", "x := 5;")
				.BindBodies()
				.Inspect("foo", st =>
				{
					var boundSt = Assert.IsType<AssignBoundStatement>(Assert.Single(Assert.IsType<SequenceBoundStatement>(st).Statements));
					var left = Assert.IsType<VariableBoundExpression>(boundSt.LeftSide);
					var right = Assert.IsType<LiteralBoundExpression>(boundSt.RightSide);
				});
		}
		[Fact]
		public void Error_AssignStatement_InvalidLValueSyntax()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo VAR x : INT; END_VAR", "(x + 5) := 5;")
				.BindBodies(ErrorOfType<CannotAssignToSyntaxMessage>());
		}

		[Fact]
		public void AssignStatement_ImplicitCast()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo VAR x : REAL; END_VAR", "x := DINT#25;")
				.BindBodies()
				.Inspect("foo", st =>
				{
					var boundSt = Assert.IsType<AssignBoundStatement>(Assert.Single(Assert.IsType<SequenceBoundStatement>(st).Statements));
					var left = Assert.IsType<VariableBoundExpression>(boundSt.LeftSide);
					var right = Assert.IsType<ImplicitArithmeticCastBoundExpression>(boundSt.RightSide);
				});
		}

		[Fact]
		public void Error_AssignStatement_IncompatibleTypes()
		{
			BindHelper.NewProject
				.AddPou("FUNCTION foo VAR x : BOOL; END_VAR", "x := 5;")
				.BindBodies(ErrorOfType<IntegerIsToLargeForTypeMessage>());
		}
	}
}
