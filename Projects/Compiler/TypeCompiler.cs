using System;
using Compiler.Messages;
using Compiler.Scopes;
using Compiler.Types;

namespace Compiler
{
	public sealed class TypeCompiler : ITypeSyntax.IVisitor<IType>
	{
		private readonly IScope Scope;
		private readonly MessageBag MessageBag;

		private TypeCompiler(IScope scope, MessageBag messageBag)
		{
			Scope = scope ?? throw new ArgumentNullException(nameof(scope));
			MessageBag = messageBag ?? throw new ArgumentNullException(nameof(messageBag));
		}

		public static IType MapSymbolic(IScope scope, ITypeSyntax syntax, MessageBag messageBag) =>
			syntax.Accept(new TypeCompiler(scope, messageBag));
		public static IType MapComplete(IScope scope, ITypeSyntax syntax, MessageBag messageBag)
		{
			var symbol = syntax.Accept(new TypeCompiler(scope, messageBag));
			DelayedLayoutType.RecursiveLayout(symbol, messageBag, syntax.SourceSpan);
			return symbol;
		}

		public IType Visit(IdentifierTypeSyntax identifierTypeSyntax)
			=> Scope.LookupType(identifierTypeSyntax.Identifier, identifierTypeSyntax.SourceSpan).Extract(MessageBag);

		public IType Visit(BuiltInTypeSyntax builtInTypeSyntax) =>
			Scope.SystemScope.BuiltInTypeTable.MapTokenToType(builtInTypeSyntax.TokenType);

		public IType Visit(ArrayTypeSyntax arrayTypeSyntax)
		{
			var baseType = arrayTypeSyntax.BaseType.Accept(this);
			var arrayTypeSymbol = new ArrayType(baseType, Scope, arrayTypeSyntax);
			return arrayTypeSymbol;
		}

		public IType Visit(PointerTypeSyntax pointerTypeSyntax)
		{
			var baseType = pointerTypeSyntax.BaseType.Accept(this);
			return new PointerType(baseType);
		}

		public IType Visit(SubrangeTypeSyntax subrangeTypeSyntax)
		{
			var baseType = subrangeTypeSyntax.BaseType.Accept(this);
			throw new NotImplementedException();
		}

		public IType Visit(StringTypeSyntax stringTypeSyntax) => new StringType(Scope, stringTypeSyntax);
		public IType Visit(ScopedIdentifierTypeSyntax scopedIdentifierTypeSyntax)
		{
			var scope = Scope.ResolveScope(scopedIdentifierTypeSyntax.Scope).Extract(MessageBag, out bool isMissingScope);
			if (isMissingScope)
				return ITypeSymbol.CreateError(scopedIdentifierTypeSyntax.TokenIdentifier.SourceSpan, scopedIdentifierTypeSyntax.Identifier);
			return scope.LookupType(scopedIdentifierTypeSyntax.Identifier, scopedIdentifierTypeSyntax.TokenIdentifier.SourceSpan).Extract(MessageBag);
		}
	}
}
