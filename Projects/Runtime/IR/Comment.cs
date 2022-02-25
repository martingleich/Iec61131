using System;

namespace Runtime.IR
{
	public sealed class Comment : IStatement
	{
		public readonly string Value;

		public Comment(string value)
		{
			Value = value ?? throw new ArgumentNullException(nameof(value));
		}

		public int? Execute(Runtime runtime) => null;
		public override string ToString() => $"# {Value}";
	}
}
