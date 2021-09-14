﻿using System;
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
		
		public static bool IsIdenticalType(IType a, IType b)
		{
			if (a is null)
				throw new ArgumentNullException(nameof(a));
			if (b is null)
				throw new ArgumentNullException(nameof(b));

			if (a is ArrayTypeSymbol arrayA && b is ArrayTypeSymbol arrayB)
				return IsIdenticalType(arrayA.BaseType, arrayB.BaseType) && Equal(arrayA.Ranges, arrayB.Ranges);
			else if (a is StringTypeSymbol stringA && b is StringTypeSymbol stringB)
				return stringA.Size == stringB.Size;
			else if (a is PointerTypeSymbol pointerA && b is PointerTypeSymbol pointerB)
				return IsIdenticalType(pointerA.BaseType, pointerB.BaseType);
			else if (a is BuiltInTypeSymbol builtInA && b is BuiltInTypeSymbol builtInB)
				return builtInA.Name == builtInB.Name;
			else if (a is EnumTypeSymbol enumA && b is EnumTypeSymbol enumB)
				return enumA.Name == enumB.Name;
			else if (a is StructuredTypeSymbol structA && b is StructuredTypeSymbol structB)
				return structA.Name == structB.Name;
			else if (a is ErrorTypeSymbol || b is ErrorTypeSymbol)
				return true;
			else
				return false;
		}
	}
}
