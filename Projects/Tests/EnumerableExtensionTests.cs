using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Compiler;
using System.Collections.Immutable;

namespace Tests
{
	public static class Ext
	{
		public static IEnumerable<T> Checked<T>(this IEnumerable<T> inner, int maxCount)
		{
			int cur = 0;
			foreach (var x in inner)
			{
				if (cur == maxCount)
					throw new InvalidOperationException("Read to many values from checked enumerable");
				yield return x;
				++cur;
			}
		}
	}
	public static class EnumerableExtensionTests
	{

		[Fact]
		public static void TryGetSingle_Null()
		{
			Assert.Throws<ArgumentNullException>(() => EnumerableExtensions.TryGetSingle<int>(null!, out _));
		}
		[Fact]
		public static void TryGetSingle_None()
		{
			Assert.False(EnumerableExtensions.TryGetSingle(Array.Empty<int>(), out _));
		}
		[Fact]
		public static void TryGetSingle_One()
		{
			Assert.True(EnumerableExtensions.TryGetSingle(new[] { 7 }, out var x));
			Assert.Equal(7, x);
		}
		[Fact]
		public static void TryGetSingle_Three()
		{
			Assert.False(EnumerableExtensions.TryGetSingle(new[] { 7, 8, 3 }.Checked(2), out _));
		}
		[Fact]
		public static void ImmutableTryGetSingle_None()
		{
			Assert.False(EnumerableExtensions.TryGetSingle(ImmutableArray<int>.Empty, out _));
		}
		[Fact]
		public static void ImmutableTryGetSingle_One()
		{
			Assert.True(EnumerableExtensions.TryGetSingle(ImmutableArray.Create(7), out var x));
			Assert.Equal(7, x);
		}
		[Fact]
		public static void ImmutableTryGetSingle_Two()
		{
			Assert.False(EnumerableExtensions.TryGetSingle(ImmutableArray.Create(7, 8), out _));
		}

		[Fact]
		public static void MoreThan_NullArg()
		{
			Assert.Throws<ArgumentNullException>(() => EnumerableExtensions.MoreThan<int>(null!, 1));
		}
		[Fact]
		public static void MoreThan_Zero()
		{
			Assert.False(EnumerableExtensions.MoreThan(Array.Empty<int>(), 1));
		}
		[Fact]
		public static void MoreThan_One()
		{
			Assert.False(EnumerableExtensions.MoreThan(new int[] { 6 }, 1));
		}
		[Fact]
		public static void MoreThan_Two()
		{
			Assert.True(EnumerableExtensions.MoreThan(new int[] { 6, 7 }.Checked(2), 1));
		}
		
		[Fact]
		public static void MoreThan_Three()
		{
			Assert.True(EnumerableExtensions.MoreThan(new int[] { 6, 7, 8 }.Checked(2), 1));
		}

		[Fact]
		public static void HasNoNullElement_Null()
		{
			Assert.Throws<ArgumentNullException>(() => EnumerableExtensions.HasNoNullElement<int>(null!, out _));
		}
		
		[Fact]
		public static void HasNoNullElement_Empty()
		{
			Assert.True(EnumerableExtensions.HasNoNullElement(Array.Empty<int>(), out var x));
			Assert.Empty(x);
		}
		
		[Fact]
		public static void HasNoNullElement_WithNull()
		{
			Assert.False(EnumerableExtensions.HasNoNullElement(new object[] { new object(), new object(), null }, out var x));
			Assert.Null(x);
		}
		[Fact]
		public static void HasNoNullElement_WithoutNull()
		{
			var input = new object[] { new object(), new object(), new object() };
			Assert.True(EnumerableExtensions.HasNoNullElement(input, out var x));
			Assert.Same(input, x);
		}
	}
}
