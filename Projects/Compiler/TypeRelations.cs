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

			public bool Visit(ITypeSymbol typeSymbol, IType context) =>
				context is ITypeSymbol other && typeSymbol.Name == other.Name;
			public bool Visit(NullType _, IType context)
				=> context is NullType;
			public bool Visit(BuiltInType builtInType, IType context)
				=> context is BuiltInType other && builtInType.Name == other.Name;
			public bool Visit(PointerType pointerType, IType context)
				=> context is PointerType other && IsIdentical(pointerType.BaseType, other.BaseType);
			public bool Visit(StringType stringType, IType context)
				=> context is StringType other && stringType.Size == other.Size;
			public bool Visit(ArrayType arrayType, IType context)
				=> context is ArrayType other && IsIdentical(arrayType.BaseType, other.BaseType) && EnumerableExtensions.Equal(arrayType.Ranges, other.Ranges);
			public bool VisitError(IType context) => true;
		}

		public static bool IsIdentical(IType a, IType b)
		{
			if (a is null)
				throw new ArgumentNullException(nameof(a));
			if (b is null)
				throw new ArgumentNullException(nameof(b));

			return a.IsError() || b.IsError() || a.Accept(IdenticalVisitor.Instance, b);
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
		public static bool IsAliasType(IType? type, [NotNullWhen(true)] out AliasTypeSymbol? aliasTypeSymbol) => IsType(type, out aliasTypeSymbol);
		public static bool IsPointerType(IType? type, [NotNullWhen(true)] out PointerType? pointerType) => IsType(type, out pointerType);
		public static bool IsArrayType(IType? type, [NotNullWhen(true)] out ArrayType? arrayType) => IsType(type, out arrayType);
		public static bool IsNullType(IType targetType) => targetType is NullType;
		public static IType ResolveAlias(IType type)
		{
			if (IsAliasType(type, out var aliasTypeSymbol))
				return ResolveAlias(aliasTypeSymbol.AliasedType);
			else
				return type;
		}
		public static IType? ResolveAliasNullable(IType? type) => type != null ? ResolveAlias(type) : null;
	}
}
