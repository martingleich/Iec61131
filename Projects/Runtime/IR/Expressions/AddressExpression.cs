using Superpower;
using Superpower.Parsers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Runtime.IR.Expressions
{
	public sealed class AddressExpression : IExpression
	{
		public readonly IBase Base;
		public readonly ImmutableArray<IElement> Elements;

		public AddressExpression(IBase @base, ImmutableArray<IElement> elements)
		{
			Base = @base ?? throw new ArgumentNullException(nameof(@base));
			Elements = elements;
		}

		public void LoadTo(RTE runtime, MemoryLocation location, int size)
		{
			var address = Base.GetValue(runtime);
			foreach (var elem in Elements)
				address = elem.Add(runtime, address);
			runtime.WriteBits(LiteralExpression.BitsFor(address), location, size);
		}

		public interface IBase
		{
			MemoryLocation GetValue(RTE runtime);
		}
		public interface IElement
		{
			MemoryLocation Add(RTE runtime, MemoryLocation location);
		}

		public sealed class BaseStackVar : IBase
		{
			public readonly LocalVarOffset Offset;

			public BaseStackVar(LocalVarOffset offset)
			{
				Offset = offset;
			}

			public MemoryLocation GetValue(RTE runtime) => runtime.LoadEffectiveAddress(Offset);
			public override string ToString() => $"{Offset}";
			public static readonly TextParser<IBase> Parser =
				from _value in LocalVarOffset.Parser
				select (IBase)new BaseStackVar(_value);
		}
		public sealed class BaseDerefStackVar : IBase
		{
			public readonly LocalVarOffset Offset;

			public BaseDerefStackVar(LocalVarOffset offset)
			{
				Offset = offset;
			}

			public MemoryLocation GetValue(RTE runtime) => runtime.LoadPointer(Offset);
			public override string ToString() => $"*{Offset}";
			public static readonly TextParser<IBase> Parser =
				from _value in Span.EqualTo("*").IgnoreThen(LocalVarOffset.Parser)
				select (IBase)new BaseDerefStackVar(_value);
		}
		public sealed class ElementOffset : IElement
		{
			private readonly ushort Offset;

			public ElementOffset(ushort offset)
			{
				Offset = offset;
			}

			public MemoryLocation Add(RTE runtime, MemoryLocation location) => new(location.Area, (ushort)(location.Offset + Offset));
			public override string ToString() => $".{Offset}";
		}
		public sealed class ElementUncheckedArray : IElement
		{
			private readonly LocalVarOffset Index;
			private readonly int Scale;

			public ElementUncheckedArray(LocalVarOffset index, int scale)
			{
				Index = index;
				Scale = scale;
			}

			public MemoryLocation Add(RTE runtime, MemoryLocation location)
			{
				int index = runtime.LoadDINT(Index);
				int newOffset = location.Offset + index * Scale;
				return new(location.Area, (ushort)newOffset);
			}

			public override string ToString() => $"[{Index},{Scale}]";
		}
		public sealed class ElementCheckedArray : IElement
		{
			private readonly int LowerBound;
			private readonly int UpperBound;
			private readonly LocalVarOffset Index;
			private readonly int Scale;

			public ElementCheckedArray(int lowerBound, int upperBound, LocalVarOffset index, int scale)
			{
				LowerBound = lowerBound;
				UpperBound = upperBound;
				Index = index;
				Scale = scale;
			}

			public MemoryLocation Add(RTE runtime, MemoryLocation location)
			{
				int index = runtime.LoadDINT(Index);
				if (index < LowerBound || index > UpperBound)
					throw runtime.Panic($"Array out of bound access. Index was {index}, valid range is [{LowerBound}, {UpperBound}].");
				int newOffset = location.Offset + (index - LowerBound) * Scale;
				return new(location.Area, (ushort)newOffset);
			}
			public override string ToString() => $"[{Index},{Scale},{LowerBound},{UpperBound}]";
		}

		public sealed class Builder
		{
			private readonly IBase Base;
			private readonly List<IElement> Elements = new();

			public Builder(IBase @base)
			{
				Base = @base ?? throw new ArgumentNullException(nameof(@base));
			}

			public void Add(IElement element) => Elements.Add(element);

			public AddressExpression GetAddressExpression() => new(Base, Elements.ToImmutableArray());
		}

		public override string ToString() => "&" + Base.ToString() + string.Join("", Elements);
		private static TextParser<IExpression> CreateParser()
		{
			var adrBase = Parse.OneOf(BaseStackVar.Parser, BaseDerefStackVar.Parser);
			var adrElementOffset = from _value in Span.EqualTo(".").IgnoreThen(ParserUtils.NaturalUInt16)
								   select (IElement)new ElementOffset(_value);
			var adrElementAnyArray = (from _index in LocalVarOffset.Parser
									  from _2 in ParserUtils.SuroundOptionalWhitespace(Span.EqualTo(","))
									  from _scale in ParserUtils.NaturalUInt16
									  from _element in (from _3 in ParserUtils.SuroundOptionalWhitespace(Span.EqualTo(","))
														from _lower in Numerics.IntegerInt32
														from _4 in ParserUtils.SuroundOptionalWhitespace(Span.EqualTo(","))
														from _upper in Numerics.IntegerInt32
														select (IElement)new ElementCheckedArray(_lower, _upper, _index, _scale))
										.OptionalOrDefault(new ElementUncheckedArray(_index, _scale))
									  select _element).Between<IElement, Superpower.Model.TextSpan>(Span.EqualTo("["), Span.EqualTo("]"));
			var adrElement = Parse.OneOf(adrElementOffset, adrElementAnyArray);
			var expressionAdr = from _1 in Span.EqualTo("&")
								from _base in adrBase
								from _elements in ParserUtils.ManyImmutable(adrElement)
								select (IExpression)new AddressExpression(_base, _elements);
			return expressionAdr;
		}
		public static readonly TextParser<IExpression> Parser = CreateParser();
	}
}
