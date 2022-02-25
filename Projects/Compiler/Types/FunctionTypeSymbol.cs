using System;

namespace Compiler.Types
{
	public readonly struct UniqueSymbolId : IEquatable<UniqueSymbolId>
	{
		internal readonly CaseInsensitiveString ModuleName;
		internal readonly CaseInsensitiveString Name;

		public UniqueSymbolId(CaseInsensitiveString moduleName, CaseInsensitiveString name)
		{
			ModuleName = moduleName;
			Name = name;
		}

		public bool Equals(UniqueSymbolId other) => other.Name == Name && other.ModuleName == ModuleName;
		public override bool Equals(object? obj) => throw new NotImplementedException();
		public override int GetHashCode() => HashCode.Combine(ModuleName, Name);
		public static bool operator ==(UniqueSymbolId left, UniqueSymbolId right) => left.Equals(right);
		public static bool operator !=(UniqueSymbolId left, UniqueSymbolId right) => !(left == right);
		public CaseInsensitiveString ToCaseInsensitive() => ToString().ToCaseInsensitive();
		public override string ToString() => $"{ModuleName}::{Name}";
	}

	public sealed class FunctionTypeSymbol : ICallableTypeSymbol
	{
		public readonly bool IsError;
		public CaseInsensitiveString Name => UniqueName.Name;
		public UniqueSymbolId UniqueName { get; }
		public SourceSpan DeclaringSpan { get; }
		public OrderedSymbolSet<ParameterVariableSymbol> Parameters { get; }
		public LayoutInfo LayoutInfo => new (0, 1);
		public string Code => UniqueName.ToString();

		public FunctionTypeSymbol(CaseInsensitiveString module, CaseInsensitiveString name, SourceSpan declaringSpan, OrderedSymbolSet<ParameterVariableSymbol> parameters) :
			this(false, module, name, declaringSpan, parameters)
		{
		}
		private FunctionTypeSymbol(bool isError, CaseInsensitiveString module, CaseInsensitiveString name, SourceSpan declaringSpan, OrderedSymbolSet<ParameterVariableSymbol> parameters)
		{
			IsError = isError;
			DeclaringSpan = declaringSpan;
			Parameters = parameters;
			UniqueName = new UniqueSymbolId(module, name);
		}

		public override string ToString() => UniqueName.ToString();

		public static FunctionTypeSymbol CreateError(SourceSpan sourceSpan)
			=> CreateError(sourceSpan, ImplicitName.ErrorFunction, ITypeSymbol.CreateErrorForFunc(sourceSpan, ImplicitName.ErrorFunction));
		public static FunctionTypeSymbol CreateError(SourceSpan sourceSpan, CaseInsensitiveString name)
			=> CreateError(sourceSpan, name, ITypeSymbol.CreateErrorForFunc(sourceSpan, name));
		public static FunctionTypeSymbol CreateError(SourceSpan sourceSpan, IType returnType)
			=> CreateError(sourceSpan, ImplicitName.ErrorFunction, returnType);
		public static FunctionTypeSymbol CreateError(SourceSpan sourceSpan, CaseInsensitiveString name, IType returnType)
			=> CreateError(sourceSpan, ImplicitName.ErrorModule, name, returnType);
		public static FunctionTypeSymbol CreateError(SourceSpan sourceSpan, CaseInsensitiveString module, CaseInsensitiveString name, IType returnType)
			=> new(true, module, name, sourceSpan, OrderedSymbolSet.ToOrderedSymbolSet(
				new ParameterVariableSymbol(ParameterKind.Output, sourceSpan, name, 0, returnType)));

		public T Accept<T, TContext>(IType.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
	}
}