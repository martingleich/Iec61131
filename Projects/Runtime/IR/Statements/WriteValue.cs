using Runtime.IR.Expressions;
using Superpower;
using Superpower.Parsers;
using System;

namespace Runtime.IR.Statements
{
	public sealed class WriteValue : IStatement
	{
		public readonly IExpression Value;
		public readonly LocalVarOffset Target;
		public readonly int Size;

		public static WriteValue WriteLiteral(ulong bits, LocalVarOffset target, int size) => new (new LiteralExpression(bits), target, size);
		public static WriteValue WriteLocal(LocalVarOffset source, LocalVarOffset target, int size) => new (new LoadValueExpression(source), target, size);

		public WriteValue(IExpression value, LocalVarOffset offset, int size)
		{
			Value = value ?? throw new ArgumentNullException(nameof(value));
			Target = offset;
			Size = size;
		}

		public int? Execute(RTE runtime)
		{
			var address = runtime.LoadEffectiveAddress(Target);
			Value.LoadTo(runtime, address, Size);
			return null;
		}

		public override string ToString() => $"copy{Size} {Value} to {Target}";

		public static readonly TextParser<IStatement> Parser =
			from _size in Span.EqualTo("copy").IgnoreThen(ParserUtils.NaturalUInt16).ThenIgnore(Span.WhiteSpace)
			from _value in IExpression.Parser
			from _1 in IR.ParserUtils.OptionalWhitespace.ThenIgnore(Span.EqualTo("to")).ThenIgnore(ParserUtils.OptionalWhitespace)
			from _deref in Span.EqualTo("*").Optional()
			from _dst in LocalVarOffset.Parser
			select (IStatement)(_deref.HasValue ? new WriteDerefValue(_value, _dst, _size) : new WriteValue(_value, _dst, _size));
	}
}
