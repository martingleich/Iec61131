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
			var x = LocalVarOffset.Parser.Parse(value);
			Assert.Equal(offset, x.Offset);
			Assert.Equal(value, x.ToString());
		}
		[Theory]
		[InlineData("stack")]
		[InlineData("blub")]
		[InlineData("stack65536")]
		public void LocalVarOffset_Bad(string value)
		{
			var x = LocalVarOffset.Parser.TryParse(value);
			Assert.False(x.HasValue);
		}

		[Fact]
		public void DerefExpression_Good()
		{
			const string source = "*stack123";
			var expr = Assert.IsType<DerefExpression>(DerefExpression.Parser.Parse(source));
			Assert.Equal(123, expr.Address.Offset);
			Assert.Equal(source, expr.ToString());
		}
		[Fact]
		public void LiteralExpression_Good()
		{
            const string source = "13452452345";
            var expr = Assert.IsType<LiteralExpression>(LiteralExpression.Parser.Parse(source));
			Assert.Equal(13452452345ul, expr.Bits);
			Assert.Equal(source, expr.ToString());
		}
		[Fact]
		public void LoadValueExpression_Good()
		{
            const string source = "stack456";
            var expr = Assert.IsType<LoadValueExpression>(LoadValueExpression.Parser.Parse(source));
			Assert.Equal(456, expr.Offset.Offset);
			Assert.Equal(source, expr.ToString());
		}
		[Fact]
		public void AddressExpression_Good_BaseStack()
		{
            const string source = "&stack7";
            var expr = Assert.IsType<AddressExpression>(AddressExpression.Parser.Parse(source));
			var @base = Assert.IsType<AddressExpression.BaseStackVar>(expr.Base);
			Assert.Equal(7, @base.Offset.Offset);
			Assert.Empty(expr.Elements);
			Assert.Equal(source, expr.ToString());
		}
		[Fact]
		public void AddressExpression_Good_BaseDerefStack()
		{
            const string source = "&*stack7";
            var expr = Assert.IsType<AddressExpression>(AddressExpression.Parser.Parse(source));
			var @base = Assert.IsType<AddressExpression.BaseDerefStackVar>(expr.Base);
			Assert.Equal(7, @base.Offset.Offset);
			Assert.Empty(expr.Elements);
			Assert.Equal(source, expr.ToString());
		}

		[Theory]
		[InlineData("*stack0", typeof(DerefExpression))]
		[InlineData("12345", typeof(LiteralExpression))]
		[InlineData("stack123", typeof(LoadValueExpression))]
		[InlineData("&stack456.7", typeof(AddressExpression))]
		public void AnyExpression_Good(string input, System.Type expectedType)
		{
            IExpression expr = IExpression.Parser.Parse(input);
            Assert.IsType(expectedType, expr);
			Assert.Equal(input, expr.ToString());
		}

		[Theory]
		[InlineData("# This is a comment\n")]
		[InlineData("# \n")]
		public void Comment_Good(string value)
		{
            IStatement result = Comment.Parser.Parse(value);
            Assert.IsType<Comment>(result);
			Assert.Equal(value, result.ToString() + "\n");
		}

		[Fact]
		public void Jump_Good()
		{
            const string source = "jump to dstLabel";
            var result = Assert.IsType<Jump>(Jump.Parser.Parse(source));
			Assert.Equal("dstLabel", result.Target.Name);
			Assert.Equal(source, result.ToString());
		}

		[Fact]
		public void JumpIfNot_Good()
		{
            const string source = "if not stack789 jump to dstLabel";
            var result = Assert.IsType<JumpIfNot>(JumpIfNot.Parser.Parse(source));
			Assert.Equal(789, result.Control.Offset);
			Assert.Equal("dstLabel", result.Target.Name);
			Assert.Equal(source, result.ToString());
		}

		[Fact]
		public void Label_Good()
		{
            const string source = "label myLabel";
            var result = Assert.IsType<Label>(Label.StatementParser.Parse(source));
			Assert.Equal("myLabel", result.Name);
			Assert.Equal(source, result.ToString());
		}

		[Fact]
		public void Return_Good()
		{
            const string source = "return";
			var parsed = Return.Parser.Parse(source);
            Assert.IsType<Return>(parsed);
			Assert.Equal(source, parsed.ToString());
		}

		[Fact]
		public void StaticCall_Good_Args0()
		{
            const string source = "call name space::func() => ";
            var parsed = Assert.IsType<StaticCall>(StaticCall.Parser.Parse(source));
			Assert.Equal("name space::func", parsed.Callee.Name);
			Assert.Empty(parsed.Inputs);
			Assert.Empty(parsed.Outputs);
			Assert.Equal(source, parsed.ToString());
		}
		[Fact]
		public void StaticCall_Good_Args1()
		{
            const string source = "call name space::func(stack1) => stack2";
            var parsed = Assert.IsType<StaticCall>(StaticCall.Parser.Parse(source));
			Assert.Collection(parsed.Inputs,
				x => Assert.Equal(1, x.Offset));
			Assert.Collection(parsed.Outputs,
				x => Assert.Equal(2, x.Offset));
			Assert.Equal(source, parsed.ToString());
		}
		[Fact]
		public void StaticCall_Good_Args2()
		{
            const string source = "call name space::func(stack1, stack2) => stack3, stack4";
            var parsed = Assert.IsType<StaticCall>(StaticCall.Parser.Parse(source));
			Assert.Collection(parsed.Inputs,
				x => Assert.Equal(1, x.Offset),
				x => Assert.Equal(2, x.Offset));
			Assert.Collection(parsed.Outputs,
				x => Assert.Equal(3, x.Offset),
				x => Assert.Equal(4, x.Offset));
			Assert.Equal(source, parsed.ToString());
		}
		[Fact]
		public void WriteValue_Good()
		{
            const string source = "copy4 777 to stack123";
            var st = Assert.IsType<WriteValue>(WriteValue.Parser.Parse(source));
			Assert.Equal(4, st.Size);
			Assert.Equal(777ul, Assert.IsType<LiteralExpression>(st.Value).Bits);
			Assert.Equal(123, st.Target.Offset);
			Assert.Equal(source, st.ToString());
		}
		[Fact]
		public void WriteDerefValue_Good()
		{
            const string source = "copy2 55 to *stack8";
            var st = Assert.IsType<WriteDerefValue>(WriteValue.Parser.Parse(source));
			Assert.Equal(2, st.Size);
			Assert.Equal(55ul, Assert.IsType<LiteralExpression>(st.Value).Bits);
			Assert.Equal(8, st.Target.Offset);
			Assert.Equal(source, st.ToString());
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
		public void AnyStatment_Good(string source, System.Type expectedType)
		{
			var statement = IStatement.Parser.Parse(source);
			Assert.IsType(expectedType, statement); ;
			Assert.Equal(source, statement.ToString());
		}
	}
}
