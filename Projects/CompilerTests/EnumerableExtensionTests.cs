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
	public static class EnumerableExtensionTests
	{
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
