using Xunit;
using Superpower;

namespace RuntimeTests
{
    using Runtime.IR.RuntimeTypes;

    public class RuntimeTypeParserTests
	{
        [Theory]
		[InlineData("ARRAY[0..10] OF INT")]
		[InlineData("ARRAY[0..10, 1..7] OF LREAL")]
		[InlineData("ARRAY[0..10, 1..7] OF ARRAY[0..0] OF INT")]
		public void Array(string source)
		{
			var parsed = Assert.IsType<RuntimeTypeArray>(IRuntimeType.Parser.Parse(source));
			Assert.Equal(parsed.Name, source);
		}
	}
}
