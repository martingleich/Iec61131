using System;

namespace Compiler.Types
{
	public sealed class BuiltInType : IType, IEquatable<BuiltInType>
	{
		public static readonly BuiltInType Char = new("Char", 1, 1);
		public static readonly BuiltInType LReal = new("LReal", 8, 8);
		public static readonly BuiltInType Real = new("Real", 4, 4);
		public static readonly BuiltInType LInt = new("LInt", 8, 8);
		public static readonly BuiltInType DInt = new("DInt", 4, 4);
		public static readonly BuiltInType Int = new("Int", 2, 2);
		public static readonly BuiltInType SInt = new("SInt", 2, 2);
		public static readonly BuiltInType ULInt = new("ULInt", 8, 8);
		public static readonly BuiltInType UDInt = new("UDInt", 4, 4);
		public static readonly BuiltInType UInt = new("UInt", 2, 2);
		public static readonly BuiltInType USInt = new("USInt", 1, 1);
		public static readonly BuiltInType LWord = new("LWord", 8, 8);
		public static readonly BuiltInType DWord = new("DWord", 4, 4);
		public static readonly BuiltInType Word = new("Word", 2, 2);
		public static readonly BuiltInType Byte = new("Byte", 1, 1);
		public static readonly BuiltInType Bool = new("Bool", 1, 1);
		public static readonly BuiltInType LTime = new("LTime", 8, 8);
		public static readonly BuiltInType Time = new("Time", 4, 4);
		public static readonly BuiltInType LDT = new("LDT", 8, 8);
		public static readonly BuiltInType DT = new("DT", 4, 4);
		public static readonly BuiltInType LDate = new("LDate", 8, 8);
		public static readonly BuiltInType Date = new("Date", 4, 4);
		public static readonly BuiltInType LTOD = new("LTOD", 8, 8);
		public static readonly BuiltInType TOD = new("TOD", 4, 4);

		public CaseInsensitiveString Name { get; }
		public string Code => Name.ToString();
		private BuiltInType(string name, int size, int alignment)
		{
			Name = name.ToCaseInsensitive();
			Size = size;
			Alignment = alignment;
		}

		public int Size { get; }
		public int Alignment { get; }
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