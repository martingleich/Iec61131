using Compiler.Types;
using System;
using System.Collections.Immutable;

namespace Compiler
{
	public static class TypeRelations
	{
		private static bool Equal<T>(ImmutableArray<T> r1, ImmutableArray<T> r2) where T:IEquatable<T>
		{
			if (r1.Length == r2.Length)
			{
				for (int i = 0; i < r1.Length; ++i)
				{
					if (!r1[i].Equals(r2[i]))
						return false;
				}
				return true;
			}
			else
			{
				return false;
			}
		}

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
				=> context is ArrayType other && IsIdentical(arrayTypeSymbol.BaseType, other.BaseType) && Equal(arrayTypeSymbol.Ranges, other.Ranges);
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

		public static bool IsAssignableTo(IType from, IType to)
		{
			if (IsIdentical(from, to))
			{
				return true;
			}
			else
			{
				if (from is PointerType && to is PointerType) // Implicit conversion between pointers
					return true;
				else
					return false;
			}
		}
	}
}
