using Xunit;
using Superpower;

namespace RuntimeTests
{
	using Runtime.IR;
	using Runtime.IR.Expressions;
	using Runtime.IR.Statements;
	public class ParserTests
	{
		[Theory]
		[InlineData("stack0", 0)]
		[InlineData("stack1234", 1234)]
		[InlineData("stack65535", 65535)]
		public void LocalVarOffset_Good(string value, int offset)
		{
			var x = Runtime.IR.LocalVarOffset.Parser.Parse(value);
			Assert.Equal(offset, x.Offset);
		}
		[Theory]
		[InlineData("stack")]
		[InlineData("blub")]
		[InlineData("stack65536")]
		public void LocalVarOffset_Bad(string value)
		{
			var x = Runtime.IR.LocalVarOffset.Parser.TryParse(value);
			Assert.False(x.HasValue);
		}

		[Fact]
		public void DerefExpression_Good()
		{
			var expr = Assert.IsType<DerefExpression>(DerefExpression.Parser.Parse("*stack123"));
			Assert.Equal(123, expr.Address.Offset);
		}
		[Fact]
		public void LiteralExpression_Good()
		{
			var expr = Assert.IsType<LiteralExpression>(LiteralExpression.Parser.Parse("13452452345"));
			Assert.Equal(13452452345ul, expr.Bits);
		}
		[Fact]
		public void LoadValueExpression_Good()
		{
			var expr = Assert.IsType<LoadValueExpression>(LoadValueExpression.Parser.Parse("stack456"));
			Assert.Equal(456, expr.Offset.Offset);
		}
		[Fact]
		public void AddressExpression_Good_BaseStack()
		{
			var expr = Assert.IsType<AddressExpression>(AddressExpression.Parser.Parse("&stack7"));
			var @base = Assert.IsType<AddressExpression.BaseStackVar>(expr.Base);
			Assert.Equal(7, @base.Offset.Offset);
			Assert.Empty(expr.Elements);
		}
		[Fact]
		public void AddressExpression_Good_BaseDerefStack()
		{
			var expr = Assert.IsType<AddressExpression>(AddressExpression.Parser.Parse("&*stack7"));
			var @base = Assert.IsType<AddressExpression.BaseDerefStackVar>(expr.Base);
			Assert.Equal(7, @base.Offset.Offset);
			Assert.Empty(expr.Elements);
		}

		[Theory]
		[InlineData("*stack0", typeof(DerefExpression))]
		[InlineData("12345", typeof(LiteralExpression))]
		[InlineData("stack123", typeof(LoadValueExpression))]
		[InlineData("&stack456.7", typeof(AddressExpression))]
		public void AnyExpression_Good(string input, System.Type expectedType)
		{
			Assert.IsType(expectedType, IExpression.Parser.Parse(input));
		}

		[Theory]
		[InlineData("# This is a comment\n")]
		[InlineData("#\n")]
		public void Comment_Good(string value)
		{
			Assert.IsType<Comment>(Comment.Parser.Parse(value));
		}

		[Fact]
		public void Jump_Good()
		{
			var result = Assert.IsType<Jump>(Jump.Parser.Parse("jump to dstLabel"));
			Assert.Equal("dstLabel", result.Target.Name);
		}

		[Fact]
		public void JumpIfNot_Good()
		{
			var result = Assert.IsType<JumpIfNot>(JumpIfNot.Parser.Parse("if not stack789 jump to dstLabel"));
			Assert.Equal(789, result.Control.Offset);
			Assert.Equal("dstLabel", result.Target.Name);
		}

		[Fact]
		public void Label_Good()
		{
			var result = Assert.IsType<Label>(Label.StatementParser.Parse("label myLabel"));
			Assert.Equal("myLabel", result.Name);
		}

		[Fact]
		public void Return_Good()
		{
			Assert.IsType<Return>(Return.Parser.Parse("return"));
		}

		[Fact]
		public void StaticCall_Good_Args0()
		{
			var parsed = Assert.IsType<StaticCall>(StaticCall.Parser.Parse("call name space::func() => "));
			Assert.Equal("name space::func", parsed.Callee.Name);
			Assert.Empty(parsed.Inputs);
			Assert.Empty(parsed.Outputs);
		}
		[Fact]
		public void StaticCall_Good_Args1()
		{
			var parsed = Assert.IsType<StaticCall>(StaticCall.Parser.Parse("call name space::func(stack1) => stack2"));
			Assert.Collection(parsed.Inputs,
				x => Assert.Equal(1, x.Offset));
			Assert.Collection(parsed.Outputs,
				x => Assert.Equal(2, x.Offset));
		}
		[Fact]
		public void StaticCall_Good_Args2()
		{
			var parsed = Assert.IsType<StaticCall>(StaticCall.Parser.Parse("call name space::func(stack1, stack2) => stack3, stack4"));
			Assert.Collection(parsed.Inputs,
				x => Assert.Equal(1, x.Offset),
				x => Assert.Equal(2, x.Offset));
			Assert.Collection(parsed.Outputs,
				x => Assert.Equal(3, x.Offset),
				x => Assert.Equal(4, x.Offset));
		}
		[Fact]
		public void WriteValue_Good()
		{
			var st = Assert.IsType<WriteValue>(WriteValue.Parser.Parse("copy4 777 to stack123"));
			Assert.Equal(4, st.Size);
			Assert.Equal(777ul, Assert.IsType<LiteralExpression>(st.Value).Bits);
			Assert.Equal(123, st.Target.Offset);
		}
		[Fact]
		public void WriteDerefValue_Good()
		{
			var st = Assert.IsType<WriteDerefValue>(WriteValue.Parser.Parse("copy2 55 to *stack8"));
			Assert.Equal(2, st.Size);
			Assert.Equal(55ul, Assert.IsType<LiteralExpression>(st.Value).Bits);
			Assert.Equal(8, st.Target.Offset);
		}

		[Theory]
		[InlineData("# Comment", typeof(Comment))]
		[InlineData("jump to dstLabel", typeof(Jump))]
		[InlineData("if not stack789 jump to dstLabel", typeof(JumpIfNot))]
		[InlineData("label myLabel", typeof(Label))]
		[InlineData("return", typeof(Return))]
		[InlineData("call name space::func(stack1) => stack2", typeof(StaticCall))]
		[InlineData("copy4 777 to stack123", typeof(WriteValue))]
		[InlineData("copy2 55 to *stack8", typeof(WriteDerefValue))]
		public void AnyStatment_Good(string input, System.Type expectedType)
		{
			Assert.IsType(expectedType, IStatement.Parser.Parse(input));
		}
	}
}
