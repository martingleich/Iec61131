using Compiler.Types;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Compiler
{
	public static class TypeRelations
	{
		private sealed class IdenticalVisitor : IType.IVisitor<bool, IType>
		{
			public static readonly IdenticalVisitor Instance = new ();
			public bool Visit(StructuredTypeSymbol structuredTypeSymbol, IType context)
				=> context is StructuredTypeSymbol other && structuredTypeSymbol.Name == other.Name;
			public bool Visit(BuiltInType builtInTypeSymbol, IType context)
				=> context is BuiltInType other && builtInTypeSymbol.Name == other.Name;
			public bool Visit(PointerType pointerTypeSymbol, IType context)
				=> context is PointerType other && IsIdentical(pointerTypeSymbol.BaseType, other.BaseType);
			public bool Visit(StringType stringTypeSymbol, IType context)
				=> context is StringType other && stringTypeSymbol.Size == other.Size;
			public bool Visit(ArrayType arrayTypeSymbol, IType context)
				=> context is ArrayType other && IsIdentical(arrayTypeSymbol.BaseType, other.BaseType) && EnumerableExtensions.Equal(arrayTypeSymbol.Ranges, other.Ranges);
			public bool Visit(EnumTypeSymbol enumTypeSymbol, IType context)
				=> context is EnumTypeSymbol other && enumTypeSymbol.Name == other.Name;
			public bool VisitError(IType context) => true;
		}

		public static bool IsIdentical(IType a, IType b)
		{
			if (a is null)
				throw new ArgumentNullException(nameof(a));
			if (b is null)
				throw new ArgumentNullException(nameof(b));

			if (a is ErrorTypeSymbol || b is ErrorTypeSymbol)
				return true;
			else
				return a.Accept(IdenticalVisitor.Instance, b);
		}

		public static IType MaxSizeType(IType a, IType b)
		{
			if (a is null)
				throw new ArgumentNullException(nameof(a));
			if (b is null)
				throw new ArgumentNullException(nameof(b));
			return a.LayoutInfo.Size > b.LayoutInfo.Size ? a : b;
		}

		private static bool IsType<T>(IType? type, [NotNullWhen(true)] out T? tType) where T : class, IType
		{
			tType = type as T;
			return tType != null;
		}
		public static bool IsBuiltInType(IType? type, [NotNullWhen(true)] out BuiltInType? builtInType) => IsType(type, out builtInType);
		public static bool IsEnumType(IType? type, [NotNullWhen(true)] out EnumTypeSymbol? enumTypeSymbol) => IsType(type, out enumTypeSymbol);
		public static bool IsPointerType(IType? type, [NotNullWhen(true)] out PointerType? pointerType) => IsType(type, out pointerType);
	}
}
