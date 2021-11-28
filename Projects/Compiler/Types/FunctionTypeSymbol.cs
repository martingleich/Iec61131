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
		public override string ToString() => $"{ModuleName}::{Name}";
	}

	public sealed class FunctionTypeSymbol : ICallableTypeSymbol
	{
		public readonly bool IsError;
		public CaseInsensitiveString Name => UniqueId.Name;
		public UniqueSymbolId UniqueId { get; }
		public SourcePosition DeclaringPosition { get; }
		public OrderedSymbolSet<ParameterVariableSymbol> Parameters { get; }
		public LayoutInfo LayoutInfo => new (0, 1);
		public string Code => Name.Original;

		public FunctionTypeSymbol(CaseInsensitiveString module, CaseInsensitiveString name, SourcePosition declaringPosition, OrderedSymbolSet<ParameterVariableSymbol> parameters) :
			this(false, module, name, declaringPosition, parameters)
		{
		}
		private FunctionTypeSymbol(bool isError, CaseInsensitiveString module, CaseInsensitiveString name, SourcePosition declaringPosition, OrderedSymbolSet<ParameterVariableSymbol> parameters)
		{
			IsError = isError;
			DeclaringPosition = declaringPosition;
			Parameters = parameters;
			UniqueId = new UniqueSymbolId(module, name);
		}

		public override string ToString() => UniqueId.ToString();

		public static FunctionTypeSymbol CreateError(SourcePosition sourcePosition)
			=> CreateError(sourcePosition, ImplicitName.ErrorFunction, ITypeSymbol.CreateErrorForFunc(sourcePosition, ImplicitName.ErrorFunction));
		public static FunctionTypeSymbol CreateError(SourcePosition sourcePosition, CaseInsensitiveString name)
			=> CreateError(sourcePosition, name, ITypeSymbol.CreateErrorForFunc(sourcePosition, name));
		public static FunctionTypeSymbol CreateError(SourcePosition sourcePosition, IType returnType)
			=> CreateError(sourcePosition, ImplicitName.ErrorFunction, returnType);
		public static FunctionTypeSymbol CreateError(SourcePosition sourcePosition, CaseInsensitiveString name, IType returnType)
			=> CreateError(sourcePosition, ImplicitName.ErrorModule, name, returnType);
		public static FunctionTypeSymbol CreateError(SourcePosition sourcePosition, CaseInsensitiveString module, CaseInsensitiveString name, IType returnType)
			=> new(true, module, name, sourcePosition, OrderedSymbolSet.ToOrderedSymbolSet(
				new ParameterVariableSymbol(ParameterKind.Output, sourcePosition, name, returnType)));

		public T Accept<T, TContext>(IType.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);
	}
}