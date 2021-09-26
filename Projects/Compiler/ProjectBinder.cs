using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Compiler.Messages;
using Compiler.Scopes;
using Compiler.Types;

namespace Compiler
{
	public sealed class BoundModule
	{
		public readonly ImmutableArray<IMessage> InterfaceMessages;
		public readonly BoundModuleInterface Interface;
		public readonly ImmutableDictionary<FunctionSymbol, BoundPou> Pous;

		public BoundModule(ImmutableArray<IMessage> interfaceMessages, BoundModuleInterface @interface, ImmutableDictionary<FunctionSymbol, BoundPou> pous)
		{
			InterfaceMessages = interfaceMessages;
			Interface = @interface ?? throw new ArgumentNullException(nameof(@interface));
			Pous = pous ?? throw new ArgumentNullException(nameof(pous));
		}
	}

	public sealed class BoundModuleInterface
	{
		public readonly SymbolSet<ITypeSymbol> DutTypes;
		public readonly SymbolSet<FunctionSymbol> FunctionSymbols;

		public BoundModuleInterface(SymbolSet<ITypeSymbol> dutTypes, SymbolSet<FunctionSymbol> functionSymbols)
		{
			DutTypes = dutTypes;
			FunctionSymbols = functionSymbols;
		}
	}

	public sealed class BoundPou
	{
		private readonly IScope Scope;
		private readonly FunctionSymbol Symbol;
		private readonly PouInterfaceSyntax Interface;
		private readonly StatementListSyntax Body;
		public Lazy<(IBoundStatement, ImmutableArray<IMessage>)> LazyBoundBody;

		public BoundPou(IScope scope, FunctionSymbol symbol, PouInterfaceSyntax @interface, StatementListSyntax body)
		{
			Scope = scope ?? throw new ArgumentNullException(nameof(scope));
			Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
			Interface = @interface ?? throw new ArgumentNullException(nameof(@interface));
			Body = body ?? throw new ArgumentNullException(nameof(body));
			LazyBoundBody = new Lazy<(IBoundStatement, ImmutableArray<IMessage>)>(Bind);
		}

		private (IBoundStatement, ImmutableArray<IMessage>) Bind()
		{
			var messageBag = new MessageBag();
			// Step 1: Create a scope for the body.
			//		a) Create a symbol set for the remaining variables.
			var localVariables = BindLocalVariables(Interface.VariableDeclarations, Scope, messageBag).ToSymbolSetWithDuplicates(messageBag);
			foreach (var local in localVariables)
			{
				if (Symbol.Parameters.TryGetValue(local.Name, out var existing))
					messageBag.Add(new SymbolAlreadyExistsMessage(local.Name, existing.DeclaringPosition, local.DeclaringPosition));
			}
			//		b) Create a statement scope A for the Symbol
			//		c) Create a statement scope B(A) with the remaining variables.
			var scope = new InsideFunctionScope(Scope, Symbol, localVariables);
			// Step 2: Bind the body
			var bound = StatementBinder.Bind(Body, scope, messageBag);
			return (bound, messageBag.ToImmutable());
		}

		private static IEnumerable<LocalVariableSymbol> BindLocalVariables(IEnumerable<VarDeclBlockSyntax> vardecls, IScope scope, MessageBag messages)
			=> vardecls.SelectMany(vardeclBlock => BindVarDeclBlock(vardeclBlock.TokenKind, vardeclBlock.Declarations, scope, messages));
		private static IEnumerable<LocalVariableSymbol> BindVarDeclBlock(IVarDeclKindToken kind, SyntaxArray<VarDeclSyntax> vardecls, IScope scope, MessageBag messages)
		{
			if (kind is not VarToken && kind is not VarTempToken)
				return Enumerable.Empty<LocalVariableSymbol>();
			else
				return vardecls.Select(v => BindVarDecl(v, scope, messages));
		}
		private static LocalVariableSymbol BindVarDecl(VarDeclSyntax syntax, IScope scope, MessageBag messages)
		{
			IType type = TypeCompiler.MapComplete(scope, syntax.Type, messages);
			return new(
				syntax.Identifiers.ToCaseInsensitive(),
				syntax.TokenIdentifiers.SourcePosition,
				type);
		}
	}

	public sealed class ProjectBinder : AInnerScope<RootScope>
	{
		private readonly SymbolSet<ITypeSymbolInWork> WorkingTypeSymbols;
		private readonly MessageBag MessageBag = new();
		private readonly PouSymbolCreatorT PouSymbolCreator;

		private ProjectBinder(
			ImmutableArray<ParsedDutLanguageSource> duts) : base(RootScope.Instance)
		{
			PouSymbolCreator = new(this, MessageBag);
			WorkingTypeSymbols = duts.ToSymbolSetWithDuplicates(MessageBag,
				v => v.Syntax.TypeBody.Accept(DutSymbolCreator.Instance, v.Syntax));
		}

		private sealed class DutSymbolCreator : ITypeDeclarationBodySyntax.IVisitor<ITypeSymbolInWork, TypeDeclarationSyntax>
		{
			public static readonly DutSymbolCreator Instance = new();
			public ITypeSymbolInWork Visit(AliasTypeDeclarationBodySyntax aliasTypeDeclarationBodySyntax, TypeDeclarationSyntax context)
			{
				throw new NotImplementedException();
			}

			public ITypeSymbolInWork Visit(StructTypeDeclarationBodySyntax structTypeDeclarationBodySyntax, TypeDeclarationSyntax context)
				=> new StructuredTypeInWork(context.TokenIdentifier.SourcePosition, context.Identifier.ToCaseInsensitive(), false, structTypeDeclarationBodySyntax.Fields);
			public ITypeSymbolInWork Visit(UnionTypeDeclarationBodySyntax unionTypeDeclarationBodySyntax, TypeDeclarationSyntax context)
				=> new StructuredTypeInWork(context.TokenIdentifier.SourcePosition, context.Identifier.ToCaseInsensitive(), true, unionTypeDeclarationBodySyntax.Fields);
			public ITypeSymbolInWork Visit(EnumTypeDeclarationBodySyntax enumTypeDeclarationBodySyntax, TypeDeclarationSyntax context)
				=> new EnumTypeInWork(context.TokenIdentifier.SourcePosition, context.Identifier.ToCaseInsensitive(), enumTypeDeclarationBodySyntax);
		}

		private sealed class PouSymbolCreatorT : IPouKindToken.IVisitor<FunctionSymbol, PouInterfaceSyntax>
		{
			private readonly IScope Scope;
			private readonly MessageBag Messages;

			public PouSymbolCreatorT(IScope scope, MessageBag messages)
			{
				Scope = scope ?? throw new ArgumentNullException(nameof(scope));
				Messages = messages ?? throw new ArgumentNullException(nameof(messages));
			}

			public FunctionSymbol ConvertToSymbol(PouInterfaceSyntax syntax) => syntax.TokenPouKind.Accept(this, syntax);

			public FunctionSymbol Visit(ProgramToken programToken, PouInterfaceSyntax context)
				=> TypifyFunctionOrProgram(isProgram: true, context);
			public FunctionSymbol Visit(FunctionToken functionToken, PouInterfaceSyntax context)
				=> TypifyFunctionOrProgram(isProgram: false, context);
			private FunctionSymbol TypifyFunctionOrProgram(bool isProgram, PouInterfaceSyntax context)
			{
				var allParameters = BindParameters(context.VariableDeclarations).Concat(BindReturnValue(context.Name.ToCaseInsensitive(), context.ReturnDeclaration));
				var uniqueParameters = allParameters.ToOrderedSymbolSetWithDuplicates(Messages);
				return new FunctionSymbol(
					isProgram,
					context.Name.ToCaseInsensitive(),
					context.TokenName.SourcePosition,
					uniqueParameters);
			}

			private IEnumerable<ParameterSymbol> BindParameters(IEnumerable<VarDeclBlockSyntax> vardecls)
				=> vardecls.SelectMany(vardeclBlock => BindVarDeclBlock(vardeclBlock.TokenKind, vardeclBlock.Declarations));
			private IEnumerable<ParameterSymbol> BindVarDeclBlock(IVarDeclKindToken kind, SyntaxArray<VarDeclSyntax> vardecls)
			{
				var mapped = ParameterKind.TryMap(kind);
				if (mapped == null)
					return Enumerable.Empty<ParameterSymbol>();
				else
					return vardecls.Select(v => BindVarDecl(mapped, v));
			}
			private ParameterSymbol BindVarDecl(ParameterKind kind, VarDeclSyntax syntax)
			{
				IType type = TypeCompiler.MapComplete(Scope, syntax.Type, Messages);
				return new(
					kind,
					syntax.TokenIdentifiers.SourcePosition,
					syntax.Identifiers.ToCaseInsensitive(),
					type);
			}
			private IEnumerable<ParameterSymbol> BindReturnValue(CaseInsensitiveString functionName, ReturnDeclSyntax? syntax)
			{
				if (syntax != null)
				{
					IType type = TypeCompiler.MapComplete(Scope, syntax.Type, Messages);
					yield return new ParameterSymbol(ParameterKind.Output, syntax.Type.SourcePosition, functionName, type);
				}
			}

			public FunctionSymbol Visit(FunctionBlockToken functionBlockToken, PouInterfaceSyntax context)
			{
				throw new NotImplementedException();
			}

			public FunctionSymbol Visit(MethodToken methodToken, PouInterfaceSyntax context)
			{
				throw new NotImplementedException();
			}
		}

		private interface ITypeSymbolInWork : ISymbol
		{
			ITypeSymbol Symbol { get; }
			ITypeSymbol CompleteSymbolic(ProjectBinder projectBinder);
		}

		private sealed class StructuredTypeInWork : ITypeSymbolInWork
		{
			private readonly StructuredTypeSymbol Symbol;
			private readonly SyntaxArray<VarDeclSyntax> FieldsSyntax;

			public StructuredTypeInWork(SourcePosition declaringPosition, CaseInsensitiveString name, bool isUnion, SyntaxArray<VarDeclSyntax> fields)
			{
				Symbol = new StructuredTypeSymbol(declaringPosition, isUnion, name);
				FieldsSyntax = fields;
			}

			ITypeSymbol ITypeSymbolInWork.Symbol => Symbol;
			public CaseInsensitiveString Name => Symbol.Name;
			public SourcePosition DeclaringPosition => Symbol.DeclaringPosition;

			public ITypeSymbol CompleteSymbolic(ProjectBinder projectBinder)
			{
				var fieldSymbols = FieldsSyntax.ToSymbolSetWithDuplicates(projectBinder.MessageBag, x => CreateFieldSymbol(projectBinder, x));
				Symbol._SetFields(fieldSymbols);
				return Symbol;
			}

			private static FieldSymbol CreateFieldSymbol(ProjectBinder projectBinder, VarDeclSyntax fieldSyntax)
			{
				var typeSymbol = TypeCompiler.MapSymbolic(projectBinder, fieldSyntax.Type, projectBinder.MessageBag);
				return new FieldSymbol(fieldSyntax.SourcePosition, fieldSyntax.Identifiers.ToCaseInsensitive(), typeSymbol);
			}
		}

		private sealed class EnumTypeInWork : ITypeSymbolInWork
		{
			private readonly EnumTypeSymbol Symbol;
			private readonly EnumTypeDeclarationBodySyntax BodySyntax;

			public EnumTypeInWork(SourcePosition declaringPosition, CaseInsensitiveString name, EnumTypeDeclarationBodySyntax bodySyntax)
			{
				Symbol = new EnumTypeSymbol(declaringPosition, name);
				BodySyntax = bodySyntax ?? throw new ArgumentNullException(nameof(bodySyntax));
			}

			ITypeSymbol ITypeSymbolInWork.Symbol => Symbol;
			public CaseInsensitiveString Name => Symbol.Name;
			public SourcePosition DeclaringPosition => Symbol.DeclaringPosition;

			public ITypeSymbol CompleteSymbolic(ProjectBinder projectBinder)
			{
				var baseType = BodySyntax.EnumBaseType != null
					? TypeCompiler.MapSymbolic(projectBinder, BodySyntax.EnumBaseType, projectBinder.MessageBag)
					: projectBinder.SystemScope.Int;
				Symbol._SetBaseType(baseType);
				List<EnumValueSymbol> allValueSymbols = new List<EnumValueSymbol>();
				EnumValueSymbol? prevSymbol = null;
				var innerScope = new InnerEnumScope(Symbol, projectBinder);
				foreach (var valueSyntax in BodySyntax.Values)
				{
					IExpressionSyntax value;
					if (valueSyntax.Value is VarInitSyntax initializerSyntax)
					{
						value = initializerSyntax.Value;
					}
					else
					{
						if (prevSymbol == null)
						{
							value = new LiteralExpressionSyntax(IntegerLiteralToken.SynthesizeEx(0, OverflowingInteger.FromUlong(0, false)));
						}
						else
						{
							value = new BinaryOperatorExpressionSyntax(
								new VariableExpressionSyntax(IdentifierToken.SynthesizeEx(0, prevSymbol.Name.Original)),
								PlusToken.Synthesize(0),
								new LiteralExpressionSyntax(IntegerLiteralToken.SynthesizeEx(0, OverflowingInteger.FromUlong(1, false))));
						}
					}
					var valueSymbol = new EnumValueSymbol(innerScope, valueSyntax.SourcePosition, valueSyntax.Identifier.ToCaseInsensitive(), value, Symbol);
					allValueSymbols.Add(valueSymbol);
					prevSymbol = valueSymbol;
				}
				var uniqueValueSymbols = allValueSymbols.ToSymbolSetWithDuplicates(projectBinder.MessageBag);
				Symbol._SetValues(uniqueValueSymbols);
				return Symbol;
			}
		}

		public static BoundModule Bind(
			ImmutableArray<ParsedTopLevelInterfaceAndBodyPouLanguageSource> pous,
			ImmutableArray<GlobalVariableLanguageSource> gvls,
			ImmutableArray<ParsedDutLanguageSource> duts)
		{
			if (gvls.Any())
				throw new NotImplementedException();
			var binder = new ProjectBinder(duts);
			return binder.Bind(pous);
		}

		private BoundModule Bind(ImmutableArray<ParsedTopLevelInterfaceAndBodyPouLanguageSource> pous)
		{
			var typeSymbols = WorkingTypeSymbols.ToSymbolSet(symbolInWork => symbolInWork.CompleteSymbolic(this));
			foreach (var typeSymbol in typeSymbols)
				DelayedLayoutType.RecursiveLayout(typeSymbol, MessageBag, typeSymbol.DeclaringPosition);
			foreach (var enumTypeSymbol in typeSymbols.OfType<EnumTypeSymbol>())
				enumTypeSymbol.RecursiveInitializers(MessageBag);

			var symbolsWithDuplicate = new List<FunctionSymbol>();
			var dictionary = ImmutableDictionary.CreateBuilder<FunctionSymbol, BoundPou>(SymbolByNameComparer<FunctionSymbol>.Instance);
			foreach (var pou in pous)
			{
				var symbol = PouSymbolCreator.ConvertToSymbol(pou.Interface);
				symbolsWithDuplicate.Add(symbol);
				var boundPou = new BoundPou(this, symbol, pou.Interface, pou.Body);
				dictionary.TryAdd(symbol, boundPou);
			}
			var functionSymbols = symbolsWithDuplicate.ToSymbolSetWithDuplicates(MessageBag);

			var itf = new BoundModuleInterface(typeSymbols, functionSymbols);
			return new BoundModule(
				MessageBag.ToImmutable(),
				itf,
				dictionary.ToImmutable());
		}

		public override ErrorsAnd<ITypeSymbol> LookupType(CaseInsensitiveString identifier, SourcePosition sourcePosition) =>
			WorkingTypeSymbols.TryGetValue(identifier, out var symbolInWork)
				? ErrorsAnd.Create(symbolInWork.Symbol)
				: base.LookupType(identifier, sourcePosition);
	}
}
