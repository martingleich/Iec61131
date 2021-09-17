using System;

namespace Compiler.Types
{
	public sealed class BuiltInType : IType, IEquatable<BuiltInType>
	{
		public static readonly BuiltInType Char = new(1, 1, "Char", isArithmetic: false, isUnsigned: false);
		public static readonly BuiltInType LReal = new(8, 8, "LReal", isArithmetic: true, isUnsigned: false);
		public static readonly BuiltInType Real = new(4, 4, "Real", isArithmetic: true, isUnsigned: false);
		public static readonly BuiltInType LInt = new(8, 8, "LInt", isArithmetic: true, isUnsigned: false);
		public static readonly BuiltInType DInt = new(4, 4, "DInt", isArithmetic: true, isUnsigned: false);
		public static readonly BuiltInType Int = new(2, 2, "Int", isArithmetic: true, isUnsigned: false);
		public static readonly BuiltInType SInt = new(2, 2, "SInt", isArithmetic: true, isUnsigned: false);
		public static readonly BuiltInType ULInt = new(8, 8, "ULInt", isArithmetic: true, isUnsigned: true);
		public static readonly BuiltInType UDInt = new(4, 4, "UDInt", isArithmetic: true, isUnsigned: true);
		public static readonly BuiltInType UInt = new(2, 2, "UInt", isArithmetic: true, isUnsigned: true);
		public static readonly BuiltInType USInt = new(1, 1, "USInt", isArithmetic: true, isUnsigned: true);
		public static readonly BuiltInType LWord = new(8, 8, "LWord", isArithmetic: false, isUnsigned: false);
		public static readonly BuiltInType DWord = new(4, 4, "DWord", isArithmetic: false, isUnsigned: false);
		public static readonly BuiltInType Word = new(2, 2, "Word", isArithmetic: false, isUnsigned: false);
		public static readonly BuiltInType Byte = new(1, 1, "Byte", isArithmetic: false, isUnsigned: false);
		public static readonly BuiltInType Bool = new(1, 1, "Bool", isArithmetic: false, isUnsigned: false);
		public static readonly BuiltInType LTime = new(8, 8, "LTime", isArithmetic: false, isUnsigned: false);
		public static readonly BuiltInType Time = new(4, 4, "Time", isArithmetic: false, isUnsigned: false);
		public static readonly BuiltInType LDT = new(8, 8, "LDT", isArithmetic: false, isUnsigned: false);
		public static readonly BuiltInType DT = new(4, 4, "DT", isArithmetic: false, isUnsigned: false);
		public static readonly BuiltInType LDate = new(8, 8, "LDate", isArithmetic: false, isUnsigned: false);
		public static readonly BuiltInType Date = new(4, 4, "Date", isArithmetic: false, isUnsigned: false);
		public static readonly BuiltInType LTOD = new(8, 8, "LTOD", isArithmetic: false, isUnsigned: false);
		public static readonly BuiltInType TOD = new(4, 4, "TOD", isArithmetic: false, isUnsigned: false);

		public CaseInsensitiveString Name { get; }
		public string Code => Name.ToString();
		private BuiltInType(int size, int alignment, string name, bool isArithmetic, bool isUnsigned)
		{
			Name = name.ToCaseInsensitive();
			Size = size;
			Alignment = alignment;
			IsArithmetic = isArithmetic;
			IsUnsigned = isUnsigned;
		}

		public int Size { get; }
		public int Alignment { get; }
		public bool IsArithmetic { get; }
		public bool IsUnsigned { get; }

		public LayoutInfo LayoutInfo => new(Size, Alignment);

		public static BuiltInType MapTokenToType(IBuiltInTypeToken token) => token.Accept(TypeMapper.Instance);
		public T Accept<T, TContext>(IType.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);

		public override string ToString() => Name.ToString();

		public bool Equals(BuiltInType? other) => other != null && other.Name == Name;
		public override int GetHashCode() => Name.GetHashCode();
		public override bool Equals(object? obj) => throw new NotImplementedException();

		private sealed class TypeMapper : IBuiltInTypeToken.IVisitor<BuiltInType>
		{
			public static readonly TypeMapper Instance = new();
			public BuiltInType Visit(CharToken charToken) => Char;
			public BuiltInType Visit(LRealToken lRealToken) => LReal;
			public BuiltInType Visit(RealToken realToken) => Real;
			public BuiltInType Visit(LIntToken lIntToken) => LInt;
			public BuiltInType Visit(DIntToken dIntToken) => DInt;
			public BuiltInType Visit(IntToken intToken) => Int;
			public BuiltInType Visit(SIntToken sIntToken) => SInt;
			public BuiltInType Visit(ULIntToken uLIntToken) => ULInt;
			public BuiltInType Visit(UDIntToken uDIntToken) => UDInt;
			public BuiltInType Visit(UIntToken uIntToken) => UInt;
			public BuiltInType Visit(USIntToken uSIntToken) => USInt;
			public BuiltInType Visit(LWordToken lWordToken) => LWord;
			public BuiltInType Visit(DWordToken dWordToken) => DWord;
			public BuiltInType Visit(WordToken wordToken) => Word;
			public BuiltInType Visit(ByteToken byteToken) => Byte;
			public BuiltInType Visit(BoolToken boolToken) => Bool;
			public BuiltInType Visit(LTimeToken lTimeToken) => LTime;
			public BuiltInType Visit(TimeToken timeToken) => Time;
			public BuiltInType Visit(LDTToken lDTToken) => LDT;
			public BuiltInType Visit(DTToken dTToken) => DT;
			public BuiltInType Visit(LDateToken lDateToken) => LDate;
			public BuiltInType Visit(DateToken dateToken) => Date;
			public BuiltInType Visit(LTODToken lTODToken) => LTOD;
			public BuiltInType Visit(TODToken tODToken) => TOD;
		}
	}
}