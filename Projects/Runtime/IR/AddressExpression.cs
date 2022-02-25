using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Runtime.IR
{
	public sealed class AddressExpression : IExpression
	{
		private readonly IBase Base;
		private readonly ImmutableArray<IElement> Elements;

		public AddressExpression(IBase @base, ImmutableArray<IElement> elements)
		{
			Base = @base ?? throw new ArgumentNullException(nameof(@base));
			Elements = elements;
		}

		public void LoadTo(Runtime runtime, MemoryLocation location, int size)
		{
			var address = Base.GetValue(runtime);
			foreach (var elem in Elements)
				address = elem.Add(runtime, address);
			runtime.Copy(LiteralExpression.BitsFor(address), location, size);
		}

		public interface IBase
		{
			MemoryLocation GetValue(Runtime runtime);
		}
		public interface IElement
		{
			MemoryLocation Add(Runtime runtime, MemoryLocation location);
		}

		public sealed class BaseStackVar : IBase
		{
			private readonly LocalVarOffset Offset;

			public BaseStackVar(LocalVarOffset offset)
			{
				Offset = offset;
			}

			public MemoryLocation GetValue(Runtime runtime) => runtime.LoadEffectiveAddress(Offset);
			public override string ToString() => $"{Offset}";
		}
		public sealed class BaseDerefStackVar : IBase
		{
			private readonly LocalVarOffset Offset;

			public BaseDerefStackVar(LocalVarOffset offset)
			{
				Offset = offset;
			}

			public MemoryLocation GetValue(Runtime runtime) => runtime.LoadPointer(Offset);
			public override string ToString() => $"*{Offset}";
		}
		public sealed class ElementOffset : IElement
		{
			private readonly ushort Offset;

			public ElementOffset(ushort offset)
			{
				Offset = offset;
			}

			public MemoryLocation Add(Runtime runtime, MemoryLocation location) => new(location.Area, (ushort)(location.Offset + Offset));
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

			public MemoryLocation Add(Runtime runtime, MemoryLocation location)
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

			public MemoryLocation Add(Runtime runtime, MemoryLocation location)
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
	}
}
