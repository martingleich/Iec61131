using System;
using System.Linq;
using Xunit;
using MessageTest = System.Action<System.Collections.Generic.IEnumerable<Compiler.Messages.IMessage>>;
using SingleMessageTest = System.Action<Compiler.Messages.IMessage>;

namespace Tests
{
	public static class ErrorHelper
	{
		public static MessageTest ExactlyMessages(params SingleMessageTest[] tests) => bag =>
		{
			Assert.Collection(bag.OrderBy(msg => msg.Span.Start), tests);
		};

		public static SingleMessageTest ErrorOfType<T>() => ErrorOfType<T>(err => { });
		public static SingleMessageTest ErrorOfType<T>(Action<T> detailed) => msg =>
		{
			var err = Assert.IsType<T>(msg);
			Assert.NotNull(msg.Text);
			Assert.True(msg.Critical);
			detailed(err);
		};
		public static SingleMessageTest WarningOfType<T>() => WarningOfType<T>(err => { });
		public static SingleMessageTest WarningOfType<T>(Action<T> detailed) => msg =>
		{
			var err = Assert.IsType<T>(msg);
			Assert.NotNull(msg.Text);
			Assert.False(msg.Critical);
			detailed(err);
		};
	}
}
