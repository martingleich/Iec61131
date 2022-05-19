using Superpower;
using System;

namespace Runtime.IR.Statements
{
	public sealed class Comment : IStatement
	{
		public readonly string Value;

		public Comment(string value)
		{
			Value = value ?? throw new ArgumentNullException(nameof(value));
		}

		public int? Execute(RTE runtime) => null;
		public override string ToString() => $"# {Value}";
		private static Comment FromParsed(string value)
		{
			return new(value[Math.Min(2, value.Length)..]);
		}
		public static readonly TextParser<IStatement> Parser =
			Superpower.Parsers.Comment.ShellStyle.Select(str => (IStatement)FromParsed(str.ToStringValue()));
	}
}
