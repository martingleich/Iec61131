using Compiler.Messages;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Compiler
{
	public interface ISymbol
	{
		public CaseInsensitiveString Name { get; }
		public SourcePosition DeclaringPosition { get; }
	}
	public interface IType
	{
		LayoutInfo LayoutInfo { get; }
		string Code { get; }
	}
	public interface ITypeSymbol : ISymbol, IType
	{
		public static ITypeSymbol CreateError(SourcePosition declaringPosition, CaseInsensitiveString name) => new ErrorTypeSymbol(declaringPosition, name);
	}

	internal interface _IDelayedLayoutType
	{
		LayoutInfo GetLayoutInfo(MessageBag messageBag, SourcePosition position);
		LayoutInfo RecursiveLayout(MessageBag messageBag, SourcePosition position);
	}
	public static class DelayedLayoutType
	{
		public static LayoutInfo GetLayoutInfo(IType typeSymbol, MessageBag messageBag, SourcePosition position)
		{
			if (typeSymbol is _IDelayedLayoutType delayedLayoutType)
				return delayedLayoutType.GetLayoutInfo(messageBag, position);
			else
				return typeSymbol.LayoutInfo;
		}
		public static LayoutInfo RecursiveLayout(IType typeSymbol, MessageBag messageBag, SourcePosition position)
		{
			if (typeSymbol is _IDelayedLayoutType delayedLayoutType)
				return delayedLayoutType.RecursiveLayout(messageBag, position);
			else
				return typeSymbol.LayoutInfo;
		}
	}
	public static class TypeExtensions
	{
		public static bool IsError(this IType self) => self is ErrorTypeSymbol;
	}

	public sealed class FieldSymbol : IVariableSymbol
	{
		public CaseInsensitiveString Name { get; }
		public SourcePosition DeclaringPosition { get; }
		private IType? _type;
		public IType Type => _type ?? throw new InvalidOperationException("Type is not initialized yet");

		public FieldSymbol(SourcePosition declaringPosition, CaseInsensitiveString name)
		{
			DeclaringPosition = declaringPosition;
			Name = name;
		}

		public FieldSymbol(SourcePosition declaringPosition, CaseInsensitiveString name, IType type)
		{
			DeclaringPosition = declaringPosition;
			Name = name;
			_type = type ?? throw new ArgumentNullException(nameof(type));
		}

		internal void _CompleteType(ITypeSymbol type)
		{
			if (_type != null)
				throw new InvalidOperationException("Type is already initialized.");
			_type = type;
		}

		public override string ToString() => $"{Name} : {Type}";
	}

	public sealed class ErrorTypeSymbol : ITypeSymbol
	{
		public ErrorTypeSymbol(SourcePosition declaringPosition, CaseInsensitiveString name)
		{
			Name = name;
			DeclaringPosition = declaringPosition;
		}

		public LayoutInfo LayoutInfo => new(0, 1);
		public CaseInsensitiveString Name { get; }
		public string Code => Name.Original;
		public SourcePosition DeclaringPosition { get; }

		public override string ToString() => Code;
	}

	public sealed class StructuredTypeSymbol : ITypeSymbol, _IDelayedLayoutType
	{
		public bool IsUnion { get; }
		public CaseInsensitiveString Name { get; }
		public string Code => Name.Original;

		public LayoutInfo? MaybeLayoutInfo { get; private set; }
		public LayoutInfo LayoutInfo => MaybeLayoutInfo!.Value;

		private SymbolSet<FieldSymbol> _fields;
		public SymbolSet<FieldSymbol> Fields => !_fields.IsDefault ? _fields : throw new InvalidOperationException("Fields is not initialized");
		public SourcePosition DeclaringPosition { get; }

		public StructuredTypeSymbol(
			SourcePosition declaringPosition,
			bool isUnion,
			CaseInsensitiveString name,
			SymbolSet<FieldSymbol> fields,
			LayoutInfo layoutInfo)
		{
			DeclaringPosition = declaringPosition;
			IsUnion = isUnion;
			Name = name;
			_fields = fields;
			MaybeLayoutInfo = layoutInfo;
			HasRecusiveLayout = true;
		}

		public override string ToString() => Name.ToString();

		internal StructuredTypeSymbol(
			SourcePosition declaringPosition,
			bool isUnion,
			CaseInsensitiveString name)
		{
			DeclaringPosition = declaringPosition;
			IsUnion = isUnion;
			Name = name;
			HasRecusiveLayout = false;
		}
		internal void _SetFields(SymbolSet<FieldSymbol> fields)
		{
			if (!_fields.IsDefault)
				throw new InvalidOperationException();
			_fields = fields;
		}

		private bool HasRecusiveLayout;
		private bool Inside_RecusiveLayout;
		LayoutInfo _IDelayedLayoutType.RecursiveLayout(MessageBag messageBag, SourcePosition position)
		{
			if (HasRecusiveLayout)
				return LayoutInfo;

			var layoutInfo = ((_IDelayedLayoutType)this).GetLayoutInfo(messageBag, position);

			if (!Inside_RecusiveLayout)
			{
				Inside_RecusiveLayout = true;
				foreach (var field in _fields)
					DelayedLayoutType.RecursiveLayout(field.Type, messageBag, field.DeclaringPosition);
				Inside_RecusiveLayout = false;
			}
			HasRecusiveLayout = true;
			return layoutInfo;
		}
		private bool Inside_GetLayoutInfo;
		LayoutInfo _IDelayedLayoutType.GetLayoutInfo(MessageBag messageBag, SourcePosition position)
		{
			if (!MaybeLayoutInfo.HasValue)
			{
				if (Inside_GetLayoutInfo)
				{
					messageBag.Add(new TypeNotCompleteMessage(position));
					MaybeLayoutInfo = LayoutInfo.Zero;
				}
				else
				{
					Inside_GetLayoutInfo = true;
					List<LayoutInfo> fieldLayouts = new List<LayoutInfo>();
					foreach (var field in _fields)
					{
						var layoutInfo = DelayedLayoutType.GetLayoutInfo(field.Type, messageBag, field.DeclaringPosition);
						fieldLayouts.Add(layoutInfo);
					}
					Inside_GetLayoutInfo = false;
					MaybeLayoutInfo = IsUnion ? LayoutInfo.Union(fieldLayouts) : LayoutInfo.Struct(fieldLayouts);
				}
			}
			return MaybeLayoutInfo.Value;
		}
	}

	public sealed class BuiltInTypeSymbol : IType
	{
		public static readonly BuiltInTypeSymbol Char = new("Char", 1, 1);
		public static readonly BuiltInTypeSymbol LReal = new("LReal", 8, 8);
		public static readonly BuiltInTypeSymbol Real = new("Real", 4, 4);
		public static readonly BuiltInTypeSymbol LInt = new("LInt", 8, 8);
		public static readonly BuiltInTypeSymbol DInt = new("DInt", 4, 4);
		public static readonly BuiltInTypeSymbol Int = new("Int", 2, 2);
		public static readonly BuiltInTypeSymbol SInt = new("SInt", 2, 2);
		public static readonly BuiltInTypeSymbol ULInt = new("ULInt", 8, 8);
		public static readonly BuiltInTypeSymbol UDInt = new("UDInt", 4, 4);
		public static readonly BuiltInTypeSymbol UInt = new("UInt", 2, 2);
		public static readonly BuiltInTypeSymbol USInt = new("USInt", 1, 1);
		public static readonly BuiltInTypeSymbol LWord = new("LWord", 8, 8);
		public static readonly BuiltInTypeSymbol DWord = new("DWord", 4, 4);
		public static readonly BuiltInTypeSymbol Word = new("Word", 2, 2);
		public static readonly BuiltInTypeSymbol Byte = new("Byte", 1, 1);
		public static readonly BuiltInTypeSymbol Bool = new("Bool", 1, 1);
		public static readonly BuiltInTypeSymbol LTime = new("LTime", 8, 8);
		public static readonly BuiltInTypeSymbol Time = new("Time", 4, 4);
		public static readonly BuiltInTypeSymbol LDT = new("LDT", 8, 8);
		public static readonly BuiltInTypeSymbol DT = new("DT", 4, 4);
		public static readonly BuiltInTypeSymbol LDate = new("LDate", 8, 8);
		public static readonly BuiltInTypeSymbol Date = new("Date", 4, 4);
		public static readonly BuiltInTypeSymbol LTOD = new("LTOD", 8, 8);
		public static readonly BuiltInTypeSymbol TOD = new("TOD", 4, 4);

		public CaseInsensitiveString Name { get; }
		public string Code => Name.ToString();
		private BuiltInTypeSymbol(string name, int size, int alignment)
		{
			Name = name.ToCaseInsensitive();
			Size = size;
			Alignment = alignment;
		}

		public int Size { get; }
		public int Alignment { get; }
		public LayoutInfo LayoutInfo => new(Size, Alignment);

		public static BuiltInTypeSymbol MapTokenToType(IBuiltInTypeToken token) => token.Accept(TypeMapper.Instance);

		public override string ToString() => Name.ToString();
		private sealed class TypeMapper : IBuiltInTypeToken.IVisitor<BuiltInTypeSymbol>
		{
			public static readonly TypeMapper Instance = new();
			public BuiltInTypeSymbol Visit(CharToken charToken) => BuiltInTypeSymbol.Char;
			public BuiltInTypeSymbol Visit(LRealToken lRealToken) => BuiltInTypeSymbol.LReal;
			public BuiltInTypeSymbol Visit(RealToken realToken) => BuiltInTypeSymbol.Real;
			public BuiltInTypeSymbol Visit(LIntToken lIntToken) => BuiltInTypeSymbol.LInt;
			public BuiltInTypeSymbol Visit(DIntToken dIntToken) => BuiltInTypeSymbol.DInt;
			public BuiltInTypeSymbol Visit(IntToken intToken) => BuiltInTypeSymbol.Int;
			public BuiltInTypeSymbol Visit(SIntToken sIntToken) => BuiltInTypeSymbol.SInt;
			public BuiltInTypeSymbol Visit(ULIntToken uLIntToken) => BuiltInTypeSymbol.ULInt;
			public BuiltInTypeSymbol Visit(UDIntToken uDIntToken) => BuiltInTypeSymbol.UDInt;
			public BuiltInTypeSymbol Visit(UIntToken uIntToken) => BuiltInTypeSymbol.UInt;
			public BuiltInTypeSymbol Visit(USIntToken uSIntToken) => BuiltInTypeSymbol.USInt;
			public BuiltInTypeSymbol Visit(LWordToken lWordToken) => BuiltInTypeSymbol.LWord;
			public BuiltInTypeSymbol Visit(DWordToken dWordToken) => BuiltInTypeSymbol.DWord;
			public BuiltInTypeSymbol Visit(WordToken wordToken) => BuiltInTypeSymbol.Word;
			public BuiltInTypeSymbol Visit(ByteToken byteToken) => BuiltInTypeSymbol.Byte;
			public BuiltInTypeSymbol Visit(BoolToken boolToken) => BuiltInTypeSymbol.Bool;
			public BuiltInTypeSymbol Visit(LTimeToken lTimeToken) => BuiltInTypeSymbol.LTime;
			public BuiltInTypeSymbol Visit(TimeToken timeToken) => BuiltInTypeSymbol.Time;
			public BuiltInTypeSymbol Visit(LDTToken lDTToken) => BuiltInTypeSymbol.LDT;
			public BuiltInTypeSymbol Visit(DTToken dTToken) => BuiltInTypeSymbol.DT;
			public BuiltInTypeSymbol Visit(LDateToken lDateToken) => BuiltInTypeSymbol.LDate;
			public BuiltInTypeSymbol Visit(DateToken dateToken) => BuiltInTypeSymbol.Date;
			public BuiltInTypeSymbol Visit(LTODToken lTODToken) => BuiltInTypeSymbol.LTOD;
			public BuiltInTypeSymbol Visit(TODToken tODToken) => BuiltInTypeSymbol.TOD;
		}
	}
	public sealed class PointerTypeSymbol : IType, _IDelayedLayoutType
	{
		public string Code => $"POINTER TO {BaseType.Code}";
		public readonly IType BaseType;

		public PointerTypeSymbol(IType baseType)
		{
			BaseType = baseType ?? throw new ArgumentNullException(nameof(baseType));
		}

		public LayoutInfo LayoutInfo => new(4, 4);

		public override string ToString() => Code;

		LayoutInfo _IDelayedLayoutType.RecursiveLayout(MessageBag messageBag, SourcePosition position)
			=> DelayedLayoutType.RecursiveLayout(BaseType, messageBag, position);
		LayoutInfo _IDelayedLayoutType.GetLayoutInfo(MessageBag messageBag, SourcePosition position)
			=> LayoutInfo;
	}
	public sealed class StringTypeSymbol : IType, _IDelayedLayoutType
	{
		private readonly StringTypeSyntax? MaybeSyntax;
		private readonly IScope? MaybeScope;

		private int? MaybeSize;
		public string Code => $"STRING[{Size}]";
		public StringTypeSymbol(IScope scope, StringTypeSyntax syntax)
		{
			MaybeScope = scope ?? throw new ArgumentNullException(nameof(scope));
			MaybeSyntax = syntax ?? throw new ArgumentNullException(nameof(syntax));
		}
		public StringTypeSymbol(int size)
		{
			MaybeSize = size;
		}
		public int Size => MaybeSize!.Value;
		public LayoutInfo LayoutInfo => new(Size, 1);
		public LayoutInfo? MaybeLayoutInfo => MaybeSize is int size ? new LayoutInfo(size, 1) : null;

		public override string ToString() => Code;

		LayoutInfo _IDelayedLayoutType.RecursiveLayout(MessageBag messageBag, SourcePosition position)
		{
			if (!MaybeSize.HasValue)
				MaybeSize = CalculateStringSize(MaybeScope!, messageBag, MaybeSyntax!.Size?.Size);
			return LayoutInfo;
		}
		private static int CalculateStringSize(IScope scope, MessageBag messageBag, IExpressionSyntax? sizeExpr)
		{
			if (sizeExpr == null)
			{
				return 80;
			}
			else
			{
				var boundSizeExpr = ExpressionBinder.BindExpression(scope, messageBag, sizeExpr, BuiltInTypeSymbol.DInt);
				var sizeValue = ConstantExpressionEvaluator.EvaluateConstant(boundSizeExpr, messageBag);
				if (sizeValue is DIntLiteralValue dintLiteralValue)
					return dintLiteralValue.Value;
				else
					return 0;
			}
		}
		LayoutInfo _IDelayedLayoutType.GetLayoutInfo(MessageBag messageBag, SourcePosition position)
		{
			return ((_IDelayedLayoutType)this).RecursiveLayout(messageBag, position);
		}
	}
	public readonly struct ArrayRange : IEquatable<ArrayRange>
	{
		public readonly int LowerBound;
		public readonly int UpperBound;

		public ArrayRange(int lowerBound, int upperBound)
		{
			if (upperBound - lowerBound + 1 < 0)
				throw new ArgumentException($"Upperbound({upperBound}) must be in range [lowerBound({lowerBound})-1, inf].", nameof(upperBound));
			LowerBound = lowerBound;
			UpperBound = upperBound;
		}

		public int Size => UpperBound - LowerBound + 1;

		public bool Equals(ArrayRange other) => LowerBound == other.LowerBound && UpperBound == other.UpperBound;
		public override bool Equals(object? obj) => obj is ArrayRange range && Equals(range);
		public override int GetHashCode() => HashCode.Combine(LowerBound, UpperBound);
		public override string ToString() => $"{LowerBound}..{UpperBound}";

		public static bool operator ==(ArrayRange left, ArrayRange right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(ArrayRange left, ArrayRange right)
		{
			return !(left == right);
		}
	}
	public sealed class ArrayTypeSymbol : IType, _IDelayedLayoutType
	{
		private readonly ArrayTypeSyntax? MaybeSyntax;
		private readonly IScope? MaybeScope;

		public CaseInsensitiveString Name => this.ToString().ToCaseInsensitive();
		public readonly IType BaseType;
		public ImmutableArray<ArrayRange> Ranges { get; private set; }
		public LayoutInfo? MaybeLayoutInfo { get; private set; }
		public LayoutInfo LayoutInfo => MaybeLayoutInfo!.Value;
		public int ElementCount => Ranges.Aggregate(1, (x, r) => x * r.Size);
		public string Code => $"ARRAY[{string.Join(", ", Ranges)}] OF {BaseType.Code}";

		public ArrayTypeSymbol(IType baseType, ImmutableArray<ArrayRange> ranges)
		{
			BaseType = baseType ?? throw new ArgumentNullException(nameof(baseType));
			Ranges = ranges;
			MaybeLayoutInfo = LayoutInfo.Array(BaseType.LayoutInfo, ElementCount);
		}

		public override string ToString() => Code;

		internal ArrayTypeSymbol(IType baseType, IScope scope, ArrayTypeSyntax declaringSyntax)
		{
			MaybeSyntax = declaringSyntax ?? throw new ArgumentNullException(nameof(declaringSyntax));
			MaybeScope = scope ?? throw new ArgumentNullException(nameof(scope));
			BaseType = baseType ?? throw new ArgumentNullException(nameof(baseType));
		}
		LayoutInfo _IDelayedLayoutType.RecursiveLayout(MessageBag messageBag, SourcePosition position)
		{
			if (MaybeLayoutInfo is LayoutInfo layoutInfo)
				return layoutInfo;
			DelayedLayoutType.RecursiveLayout(BaseType, messageBag, MaybeSyntax!.BaseType.SourcePosition);
			Ranges = CalculateArrayRanges(MaybeScope!, messageBag, MaybeSyntax!);
			MaybeLayoutInfo = LayoutInfo.Array(BaseType.LayoutInfo, ElementCount);
			return MaybeLayoutInfo.Value;
		}
		LayoutInfo _IDelayedLayoutType.GetLayoutInfo(MessageBag messageBag, SourcePosition position)
		{
			((_IDelayedLayoutType)this).RecursiveLayout(messageBag, position);
			return this.LayoutInfo;
		}

		private struct BoundRange<T> where T : class, ILiteralValue
		{
			public readonly RangeSyntax Syntax;
			public readonly T? Lower;
			public readonly T? Upper;

			public BoundRange(T? lower, T? upper, RangeSyntax syntax)
			{
				Lower = lower;
				Upper = upper;
				Syntax = syntax ?? throw new ArgumentNullException(nameof(syntax));
			}
		}
		private static BoundRange<T> BindRange<T>(IScope scope, MessageBag messageBag, RangeSyntax syntax, IType type) where T : class, ILiteralValue
		{
			var lowerbound = ConstantExpressionEvaluator.EvaluateConstant(scope, messageBag, type, syntax.LowerBound) as T;
			var upperbound = ConstantExpressionEvaluator.EvaluateConstant(scope, messageBag, type, syntax.UpperBound) as T;
			return new BoundRange<T>(lowerbound, upperbound, syntax);
		}
		private static ArrayRange BoundRangeToArrayRange(MessageBag messageBag, BoundRange<DIntLiteralValue> range)
		{
			if (range.Lower == null && range.Upper != null)
				return new ArrayRange(range.Upper.Value, range.Upper.Value);
			else if (range.Lower != null && range.Upper == null)
				return new ArrayRange(range.Lower.Value, range.Lower.Value);
			else if (range.Lower != null && range.Upper != null)
			{
				if (range.Upper.Value - range.Lower.Value + 1 < 0)
				{
					messageBag.Add(new InvalidArrayRangesMessages(range.Syntax.SourcePosition));
					return new ArrayRange(range.Lower.Value, range.Lower.Value);
				}
				else
				{
					return new ArrayRange(range.Lower.Value, range.Upper.Value);
				}
			}
			else
			{
				return new ArrayRange(0, 0);
			}
		}
		private static ImmutableArray<ArrayRange> CalculateArrayRanges(IScope scope, MessageBag messageBag, ArrayTypeSyntax arraySyntax)
			=> arraySyntax.Ranges.Select(r => BindRange<DIntLiteralValue>(scope, messageBag, r, BuiltInTypeSymbol.DInt)).Select(x => BoundRangeToArrayRange(messageBag, x)).ToImmutableArray();

	}

	public interface IVariableSymbol : ISymbol
	{
		IType Type { get; }

		public static IVariableSymbol CreateError(SourcePosition declaringPosition, CaseInsensitiveString name) =>
			new ErrorVariableSymbol(declaringPosition, ITypeSymbol.CreateError(declaringPosition, name), name);
	}

	public sealed class ErrorVariableSymbol : IVariableSymbol
	{
		public ErrorVariableSymbol(SourcePosition declaringPosition, IType type, CaseInsensitiveString name)
		{
			DeclaringPosition = declaringPosition;
			Type = type;
			Name = name;
		}

		public IType Type { get; }
		public CaseInsensitiveString Name { get; }
		public SourcePosition DeclaringPosition { get; }
	}

	public sealed class EnumValueSymbol : IVariableSymbol
	{
		public SourcePosition DeclaringPosition { get; }
		public CaseInsensitiveString Name { get; }
		private EnumLiteralValue? _value;
		public EnumLiteralValue Value => _value ?? throw new InvalidOperationException("Value is not initialised yet");
		IType IVariableSymbol.Type => Type;
		public readonly EnumTypeSymbol Type;

		public EnumValueSymbol(SourcePosition declaringPosition, CaseInsensitiveString name, EnumLiteralValue value)
		{
			DeclaringPosition = declaringPosition;
			Name = name;
			_value = value ?? throw new ArgumentNullException(nameof(value));
			Type = value.Type;
		}

		public override string ToString() => $"{Name} = {Value.InnerValue}";

		private readonly IExpressionSyntax? MaybeValueSyntax;
		private readonly IScope? MaybeScope;
		internal EnumValueSymbol(IScope scope, SourcePosition declaringPosition, CaseInsensitiveString name, IExpressionSyntax value, EnumTypeSymbol enumTypeSymbol)
		{
			MaybeScope = scope ?? throw new ArgumentNullException(nameof(scope));
			DeclaringPosition = declaringPosition;
			Name = name;
			MaybeValueSyntax = value ?? throw new ArgumentNullException(nameof(value));
			Type = enumTypeSymbol ?? throw new ArgumentNullException(nameof(enumTypeSymbol));
		}

		private bool InGetConstantValue;
		internal ILiteralValue _GetConstantValue(MessageBag messageBag, SourcePosition sourcePosition)
		{
			if (_value != null)
			{
				return _value;
			}

			if (InGetConstantValue)
			{
				messageBag.Add(new RecursiveConstantDeclarationMessage(sourcePosition));
				return _value = new EnumLiteralValue(Type, new UnknownLiteralValue(Type.BaseType));
			}

			InGetConstantValue = true;
			var boundExpression = ExpressionBinder.BindExpression(MaybeScope!, messageBag, MaybeValueSyntax!, Type.BaseType);
			var literalValue = ConstantExpressionEvaluator.EvaluateConstant(boundExpression, messageBag);
			InGetConstantValue = false;
			return _value = new EnumLiteralValue(Type, literalValue!);
		}
	}

	public sealed class EnumTypeSymbol : ITypeSymbol, _IDelayedLayoutType
	{
		public CaseInsensitiveString Name { get; }
		public string Code => Name.Original;
		public LayoutInfo LayoutInfo => BaseType.LayoutInfo;
		private IType? _baseType;
		public IType BaseType => _baseType ?? throw new InvalidOperationException("BaseType is not initialized yet");
		private SymbolSet<EnumValueSymbol> _values;
		public SymbolSet<EnumValueSymbol> Values => _values.IsDefault
			? throw new InvalidOperationException("Elements is not initialzed yet")
			: _values;

		public SourcePosition DeclaringPosition { get; }

		public EnumTypeSymbol(SourcePosition declaringPosition, CaseInsensitiveString name, IType baseType, SymbolSet<EnumValueSymbol> values)
		{
			DeclaringPosition = declaringPosition;
			Name = name;
			_baseType = baseType ?? throw new ArgumentNullException(nameof(baseType));
			_values = values;
		}

		internal EnumTypeSymbol(SourcePosition declaringPosition, CaseInsensitiveString name)
		{
			DeclaringPosition = declaringPosition;
			Name = name;
		}
		internal void _SetBaseType(IType baseType)
		{
			_baseType = baseType ?? throw new ArgumentNullException(nameof(baseType));
		}
		internal void _SetValues(SymbolSet<EnumValueSymbol> values)
		{
			_values = values;
		}

		public override string ToString() => Name.ToString();

		public LayoutInfo GetLayoutInfo(MessageBag messageBag, SourcePosition position)
		{
			return DelayedLayoutType.GetLayoutInfo(BaseType, messageBag, position);
		}

		public LayoutInfo RecursiveLayout(MessageBag messageBag, SourcePosition position)
		{
			return DelayedLayoutType.RecursiveLayout(BaseType, messageBag, position);
		}

		public void RecursiveInitializers(MessageBag messageBag, SourcePosition position)
		{
			foreach (var value in Values)
				value._GetConstantValue(messageBag, position);
		}
	}
}
