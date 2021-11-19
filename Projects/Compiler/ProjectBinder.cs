using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Compiler.Messages;
using Compiler.Scopes;
using Compiler.Types;
using StandardLibraryExtensions;

namespace Compiler
{
	public sealed class BoundModule
	{
		public readonly ImmutableArray<IMessage> InterfaceMessages;
		public readonly BoundModuleInterface Interface;
		public readonly ImmutableDictionary<FunctionVariableSymbol, BoundPou> FunctionPous;
		public readonly ImmutableDictionary<FunctionBlockSymbol, BoundPou> FunctionBlockPous;

		public BoundModule(
			ImmutableArray<IMessage> interfaceMessages,
			BoundModuleInterface @interface,
			ImmutableDictionary<FunctionVariableSymbol, BoundPou> functionPous,
			ImmutableDictionary<FunctionBlockSymbol, BoundPou> functionBlockPous)
		{
			InterfaceMessages = interfaceMessages;
			Interface = @interface ?? throw new ArgumentNullException(nameof(@interface));
			FunctionPous = functionPous ?? throw new ArgumentNullException(nameof(functionPous));
			FunctionBlockPous = functionBlockPous ?? throw new ArgumentNullException(nameof(functionBlockPous));
		}
	}

	public sealed class BoundModuleInterface
	{
		public readonly SymbolSet<ITypeSymbol> Types;
		public readonly SymbolSet<FunctionVariableSymbol> FunctionSymbols;
		public readonly SymbolSet<GlobalVariableListSymbol> GlobalVariableListSymbols;

		public readonly SymbolSet<IScopeSymbol> Scopes;

		public BoundModuleInterface(
			SymbolSet<ITypeSymbol> types,
			SymbolSet<FunctionVariableSymbol> functionSymbols,
			SymbolSet<GlobalVariableListSymbol> globalVariableListSymbols,
			SymbolSet<IScopeSymbol> scopes)
		{
			Types = types;
			FunctionSymbols = functionSymbols;
			GlobalVariableListSymbols = globalVariableListSymbols;
			Scopes = scopes;
		}
	}

	public sealed class BoundPou
	{
		public readonly Lazy<(IBoundStatement, ImmutableArray<IMessage>)> LazyBoundBody;

		public BoundPou(Lazy<(IBoundStatement, ImmutableArray<IMessage>)> lazyBoundBody)
		{
			LazyBoundBody = lazyBoundBody ?? throw new ArgumentNullException(nameof(lazyBoundBody));
		}

		private static readonly object? Marker = new();
		static IEnumerable<LocalVariableSymbol> BindLocalVariables(SyntaxArray<VarDeclBlockSyntax> vardecls, IScope scope, Func<IVarDeclKindToken, bool> isLocal, MessageBag messages)
			=> ProjectBinder.BindVariableBlocks(vardecls, scope, messages,
				kind => isLocal(kind) ? Marker : null,
				(_, scope, bag, syntax) =>
				{
					IType type = TypeCompiler.MapComplete(scope, syntax.Type, messages);
					return new LocalVariableSymbol(
						syntax.TokenIdentifier.SourcePosition,
						syntax.Identifier,
						type);
				});
		public static BoundPou FromFunction(IScope scope, PouInterfaceSyntax @interface, StatementListSyntax body, FunctionTypeSymbol symbol)
		{
			(IBoundStatement, ImmutableArray<IMessage>) Bind()
			{
				var messageBag = new MessageBag();
				var localVariables = BindLocalVariables(@interface.VariableDeclarations, scope, token => token is VarToken || token is VarTempToken, messageBag).ToSymbolSetWithDuplicates(messageBag);
				foreach (var local in localVariables)
				{
					if (symbol.Parameters.TryGetValue(local.Name, out var existing))
						messageBag.Add(new SymbolAlreadyExistsMessage(local.Name, existing.DeclaringPosition, local.DeclaringPosition));
				}
				var innerScope = new TemporaryVariablesScope(new InsideCallableScope(scope, symbol), localVariables);
				var bound = StatementBinder.Bind(body, innerScope, messageBag);
				return (bound, messageBag.ToImmutable());
			}

			return new BoundPou(new Lazy<(IBoundStatement, ImmutableArray<IMessage>)>(Bind));
		}

		public static BoundPou FromFunctionBlock(IScope scope, PouInterfaceSyntax @interface, StatementListSyntax body, FunctionBlockSymbol symbol)
		{
			(IBoundStatement, ImmutableArray<IMessage>) Bind()
			{
				var messageBag = new MessageBag();
				var localVariables = BindLocalVariables(@interface.VariableDeclarations, scope, token => token is VarTempToken, messageBag).ToSymbolSetWithDuplicates(messageBag);
				foreach (var local in localVariables)
				{
					if (symbol.Parameters.TryGetValue(local.Name, out var existingParameter))
						messageBag.Add(new SymbolAlreadyExistsMessage(local.Name, existingParameter.DeclaringPosition, local.DeclaringPosition));
					if (symbol.Fields.TryGetValue(local.Name, out var existingField))
						messageBag.Add(new SymbolAlreadyExistsMessage(local.Name, existingField.DeclaringPosition, local.DeclaringPosition));
				}
				var innerScope = new TemporaryVariablesScope(new InsideTypeScope(new InsideCallableScope(scope, symbol), symbol.Fields), localVariables);
				var bound = StatementBinder.Bind(body, innerScope, messageBag);
				return (bound, messageBag.ToImmutable());
			}

			return new BoundPou(new Lazy<(IBoundStatement, ImmutableArray<IMessage>)>(Bind));
		}
	}

	public static class ProjectBinder
	{
		private sealed class Scope : AInnerScope<RootScope>
		{
			public readonly SymbolSet<ITypeSymbol> TypeSymbols;
			public readonly SymbolSet<GlobalVariableListSymbol> GvlSymbols;
			public readonly SymbolSet<FunctionVariableSymbol> WorkingFunctionSymbols;

			public Scope(
				SymbolSet<ITypeSymbol> workingTypeSymbols,
				SymbolSet<GlobalVariableListSymbol> workingGvlSymbols,
				SymbolSet<FunctionVariableSymbol> workingFunctionSymbols,
				RootScope outerScope) : base(outerScope)
			{
				TypeSymbols = workingTypeSymbols;
				GvlSymbols = workingGvlSymbols;
				WorkingFunctionSymbols = workingFunctionSymbols;
			}

			public override ErrorsAnd<ITypeSymbol> LookupType(CaseInsensitiveString identifier, SourcePosition sourcePosition) =>
				TypeSymbols.TryGetValue(identifier, out var symbolInWork)
					? ErrorsAnd.Create(symbolInWork)
					: base.LookupType(identifier, sourcePosition);
			public override ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourcePosition sourcePosition) =>
				WorkingFunctionSymbols.TryGetValue(identifier, out var symbolInWork)
					? ErrorsAnd.Create<IVariableSymbol>(symbolInWork)
					: base.LookupVariable(identifier, sourcePosition);
			public override ErrorsAnd<IScopeSymbol> LookupScope(CaseInsensitiveString identifier, SourcePosition sourcePosition) =>
				GvlSymbols.TryGetValue(identifier, out var symbol)
					? ErrorsAnd.Create<IScopeSymbol>(symbol)
					: base.LookupScope(identifier, sourcePosition);
		}

		private sealed class DutSymbolCreator : ITypeDeclarationBodySyntax.IVisitor<ITypeSymbolInWork, TypeDeclarationSyntax>
		{
			public static readonly DutSymbolCreator Instance = new();
			public ITypeSymbolInWork Visit(AliasTypeDeclarationBodySyntax aliasTypeDeclarationBodySyntax, TypeDeclarationSyntax context)
				=> new AliasTypeInWork(context.TokenIdentifier.SourcePosition, context.Identifier, aliasTypeDeclarationBodySyntax.BaseType);
			public ITypeSymbolInWork Visit(StructTypeDeclarationBodySyntax structTypeDeclarationBodySyntax, TypeDeclarationSyntax context)
				=> new StructuredTypeInWork(context.TokenIdentifier.SourcePosition, context.Identifier, false, structTypeDeclarationBodySyntax.Fields);
			public ITypeSymbolInWork Visit(UnionTypeDeclarationBodySyntax unionTypeDeclarationBodySyntax, TypeDeclarationSyntax context)
				=> new StructuredTypeInWork(context.TokenIdentifier.SourcePosition, context.Identifier, true, unionTypeDeclarationBodySyntax.Fields);
			public ITypeSymbolInWork Visit(EnumTypeDeclarationBodySyntax enumTypeDeclarationBodySyntax, TypeDeclarationSyntax context)
				=> new EnumTypeInWork(context.TokenIdentifier.SourcePosition, context.Identifier, enumTypeDeclarationBodySyntax);
		}

		private sealed class PouFunctionSymbolCreatorT : IPouKindToken.IVisitor<FunctionTypeSymbol?, PouInterfaceSyntax>
		{
			private readonly IScope Scope;
			private readonly MessageBag Messages;

			public PouFunctionSymbolCreatorT(IScope scope, MessageBag messages)
			{
				Scope = scope ?? throw new ArgumentNullException(nameof(scope));
				Messages = messages ?? throw new ArgumentNullException(nameof(messages));
			}

			public FunctionTypeSymbol? ConvertToSymbol(PouInterfaceSyntax syntax) => syntax.TokenPouKind.Accept(this, syntax);

			public FunctionTypeSymbol? Visit(ProgramToken programToken, PouInterfaceSyntax context)
				=> TypifyFunctionOrProgram(context);
			public FunctionTypeSymbol? Visit(FunctionToken functionToken, PouInterfaceSyntax context)
				=> TypifyFunctionOrProgram(context);
			public FunctionTypeSymbol? Visit(FunctionBlockToken functionBlockToken, PouInterfaceSyntax context) => null;

			private FunctionTypeSymbol TypifyFunctionOrProgram(PouInterfaceSyntax context)
			{
				OrderedSymbolSet<ParameterVariableSymbol> uniqueParameters = BindParameters(Scope, Messages, context);
				return new FunctionTypeSymbol(
					context.Name,
					context.TokenName.SourcePosition,
					uniqueParameters);
			}

		}

		private sealed class PouTypeSymbolCreatorT : IPouKindToken.IVisitor<ITypeSymbolInWork?, ParsedTopLevelInterfaceAndBodyPouLanguageSource>
		{
			public static readonly PouTypeSymbolCreatorT Instance = new();
			public ITypeSymbolInWork? Visit(ProgramToken programToken, ParsedTopLevelInterfaceAndBodyPouLanguageSource context) => null;
			public ITypeSymbolInWork? Visit(FunctionToken functionToken, ParsedTopLevelInterfaceAndBodyPouLanguageSource context) => null;
			public ITypeSymbolInWork? Visit(FunctionBlockToken functionBlockToken, ParsedTopLevelInterfaceAndBodyPouLanguageSource context) =>
				new FunctionBlockTypeInWork(context.Interface, context.Body);
		}

		private interface ITypeSymbolInWork : ISymbol
		{
			ITypeSymbol Symbol { get; }
			ITypeSymbol CompleteSymbolic(IScope scope, MessageBag messageBag);
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

			public ITypeSymbol CompleteSymbolic(IScope scope, MessageBag messageBag)
			{
				var fieldSymbols = FieldsSyntax.ToSymbolSetWithDuplicates(messageBag, x => CreateFieldSymbol(scope, messageBag, x));
				Symbol.InternalSetFields(fieldSymbols);
				return Symbol;
			}

			private static FieldVariableSymbol CreateFieldSymbol(IScope scope, MessageBag messageBag, VarDeclSyntax fieldSyntax)
			{
				var typeSymbol = TypeCompiler.MapSymbolic(scope, fieldSyntax.Type, messageBag);
				return new FieldVariableSymbol(fieldSyntax.SourcePosition, fieldSyntax.Identifier, typeSymbol);
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

			public ITypeSymbol CompleteSymbolic(IScope scope, MessageBag messageBag)
			{
				var baseType = BodySyntax.EnumBaseType != null
					? TypeCompiler.MapSymbolic(scope, BodySyntax.EnumBaseType, messageBag)
					: scope.SystemScope.Int;
				Symbol._SetBaseType(baseType);
				List<EnumVariableSymbol> allValueSymbols = new List<EnumVariableSymbol>();
				EnumVariableSymbol? prevSymbol = null;
				var innerScope = new InsideEnumScope(Symbol, scope);
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
								new VariableExpressionSyntax(IdentifierToken.SynthesizeEx(0, prevSymbol.Name)),
								PlusToken.Synthesize(0),
								new LiteralExpressionSyntax(IntegerLiteralToken.SynthesizeEx(0, OverflowingInteger.FromUlong(1, false))));
						}
					}
					var valueSymbol = new EnumVariableSymbol(innerScope, valueSyntax.SourcePosition, valueSyntax.Identifier, value, Symbol);
					allValueSymbols.Add(valueSymbol);
					prevSymbol = valueSymbol;
				}
				var uniqueValueSymbols = allValueSymbols.ToSymbolSetWithDuplicates(messageBag);
				Symbol._SetValues(uniqueValueSymbols);
				return Symbol;
			}
		}

		private sealed class AliasTypeInWork : ITypeSymbolInWork
		{
			private readonly AliasTypeSymbol Symbol;
			private readonly ITypeSyntax AliasedTypeSyntax;

			public AliasTypeInWork(SourcePosition declaringPosition, CaseInsensitiveString name, ITypeSyntax aliasedTypeSyntax)
			{
				Symbol = new AliasTypeSymbol(declaringPosition, name);
				AliasedTypeSyntax = aliasedTypeSyntax;
			}

			ITypeSymbol ITypeSymbolInWork.Symbol => Symbol;
			public CaseInsensitiveString Name => Symbol.Name;
			public SourcePosition DeclaringPosition => Symbol.DeclaringPosition;

			public ITypeSymbol CompleteSymbolic(IScope scope, MessageBag messageBag)
			{
				Symbol._SetAliasedType(TypeCompiler.MapSymbolic(scope, AliasedTypeSyntax, messageBag));
				return Symbol;
			}
		}

		private sealed class FunctionBlockTypeInWork : ITypeSymbolInWork
		{
			private readonly PouInterfaceSyntax Syntax;
			private readonly StatementListSyntax BodySyntax;

			public FunctionBlockTypeInWork(PouInterfaceSyntax syntax, StatementListSyntax body)
			{
				Syntax = syntax ?? throw new ArgumentNullException(nameof(syntax));
				BodySyntax = body ?? throw new ArgumentNullException(nameof(body));
				Symbol = new FunctionBlockSymbol(syntax.SourcePosition, syntax.Name);
			}

			public FunctionBlockSymbol Symbol { get; }
			ITypeSymbol ITypeSymbolInWork.Symbol => Symbol;
			public CaseInsensitiveString Name => Symbol.Name;
			public SourcePosition DeclaringPosition => Symbol.DeclaringPosition;
			public ITypeSymbol CompleteSymbolic(IScope scope, MessageBag messageBag)
			{
				var parameters = BindParameters(scope, messageBag, Syntax);
				Symbol._SetParameters(parameters);
				var fields = BindFields(scope, messageBag, Syntax.VariableDeclarations).ToSymbolSetWithDuplicates(messageBag);
				Symbol._SetFields(fields);
				foreach (var field in fields)
				{
					if (parameters.TryGetValue(field.Name, out var original))
					{
						messageBag.Add(new SymbolAlreadyExistsMessage(field.Name, original.DeclaringPosition, field.DeclaringPosition));
					}
				}
				return Symbol;
			}
			public BoundPou GetBoundPou(IScope moduleScope)
				=> BoundPou.FromFunctionBlock(moduleScope, Syntax, BodySyntax, Symbol);
			private static IEnumerable<FieldVariableSymbol> BindFields(IScope scope, MessageBag messageBag, SyntaxArray<VarDeclBlockSyntax> vardecls)
				=> BindVariableBlocks(vardecls, scope, messageBag,
					kindToken => kindToken as VarToken,
					(_, scope, bag, syntax) =>
					{
						IType type = TypeCompiler.MapSymbolic(scope, syntax.Type, bag);
						return new FieldVariableSymbol(
							syntax.TokenIdentifier.SourcePosition,
							syntax.Identifier,
							type);
					});

		}

		private static class GvlSymbolInWork
		{
			public static GlobalVariableListSymbol Create(ParsedGVLLanguageSource x, IScope scope, MessageBag messageBag)
			{
				var variables = BindVariables(x.Syntax.VariableDeclarations, scope, messageBag).ToSymbolSetWithDuplicates(messageBag);
				return new GlobalVariableListSymbol(x.Syntax.SourcePosition, x.Name, variables);
			}

			private static IEnumerable<GlobalVariableSymbol> BindVariables(IEnumerable<VarDeclBlockSyntax> vardecls, IScope scope, MessageBag messages)
				=> vardecls.SelectMany(vardeclBlock => BindVarDeclBlock(vardeclBlock.TokenKind, vardeclBlock.Declarations, scope, messages));
			private static IEnumerable<GlobalVariableSymbol> BindVarDeclBlock(IVarDeclKindToken kind, SyntaxArray<VarDeclSyntax> vardecls, IScope scope, MessageBag messages)
			{
				if (kind is not VarGlobalToken)
					messages.Add(new OnlyVarGlobalInGvlMessage(kind.SourcePosition));
				return vardecls.Select(v => BindVarDecl(v, scope, messages));
			}
			private static GlobalVariableSymbol BindVarDecl(VarDeclSyntax syntax, IScope scope, MessageBag messages)
			{
				IType type = TypeCompiler.MapSymbolic(scope, syntax.Type, messages);
				return new(
					syntax.TokenIdentifier.SourcePosition,
					syntax.Identifier,
					type);
			}
		}

		private sealed class FunctionSymbolInWork : ISymbol
		{
			public FunctionSymbolInWork(FunctionTypeSymbol symbol, PouInterfaceSyntax interfaceSyntax, StatementListSyntax bodySyntax)
			{
				VariableSymbol = new FunctionVariableSymbol(symbol);
				InterfaceSyntax = interfaceSyntax ?? throw new ArgumentNullException(nameof(interfaceSyntax));
				BodySyntax = bodySyntax ?? throw new ArgumentNullException(nameof(bodySyntax));
			}
			public FunctionTypeSymbol Symbol => VariableSymbol.Type;
			public FunctionVariableSymbol VariableSymbol { get; }

			private readonly PouInterfaceSyntax InterfaceSyntax;
			private readonly StatementListSyntax BodySyntax;

			public CaseInsensitiveString Name => ((ISymbol)Symbol).Name;
			public SourcePosition DeclaringPosition => ((ISymbol)Symbol).DeclaringPosition;

			public BoundPou GetBoundPou(IScope moduleScope)
				=> BoundPou.FromFunction(moduleScope, InterfaceSyntax, BodySyntax, Symbol);
		}

		public static BoundModule Bind(
			ImmutableArray<ParsedTopLevelInterfaceAndBodyPouLanguageSource> pous,
			ImmutableArray<ParsedGVLLanguageSource> gvls,
			ImmutableArray<ParsedDutLanguageSource> duts)
		{
			var rootScope = new RootScope(new SystemScope());
			var messageBag = new MessageBag();

			var dutSymbols = ImmutableArray.CreateRange(duts, dut => dut.Syntax.TypeBody.Accept(DutSymbolCreator.Instance, dut.Syntax));
			var fbTypeSymbols = pous.Select(pou => pou.Interface.TokenPouKind.Accept(PouTypeSymbolCreatorT.Instance, pou)).WhereNotNull().ToImmutableArray();
			var workingTypeSymbols = dutSymbols.Concat(fbTypeSymbols).ToSymbolSetWithDuplicates(messageBag);
			var typeScope = new Scope(
				workingTypeSymbols.ToSymbolSet(t => t.Symbol),
				SymbolSet<GlobalVariableListSymbol>.Empty,
				SymbolSet<FunctionVariableSymbol>.Empty,
				rootScope);
			var workingGvlSymbols = gvls.ToSymbolSetWithDuplicates(messageBag,
				x => GvlSymbolInWork.Create(x, typeScope, messageBag));
			var typeFuncScope = new Scope(
				typeScope.TypeSymbols,
				workingGvlSymbols,
				SymbolSet<FunctionVariableSymbol>.Empty,
				rootScope);

			var pouFunctionSymbolCreator = new PouFunctionSymbolCreatorT(typeFuncScope, messageBag);
			var workingFunctionSymbols = (from pou in pous
										  let symbol = pouFunctionSymbolCreator.ConvertToSymbol(pou.Interface)
										  where symbol != null
										  select new FunctionSymbolInWork(symbol, pou.Interface, pou.Body)).ToSymbolSetWithDuplicates(messageBag);
			var fullScope = new Scope(
				typeFuncScope.TypeSymbols,
				typeFuncScope.GvlSymbols,
				workingFunctionSymbols.ToSymbolSet(f => f.VariableSymbol),
				rootScope);

			////////////////////////////////////////////////////////////
			// Mutate all symbols
			// To add sizes, and corrected layout.
			CompleteSymbols(
				fullScope,
				messageBag,
				workingTypeSymbols,
				workingGvlSymbols,
				workingFunctionSymbols);
			// After this point all symbols are frozen
			////////////////////////////////////////////////////////////

			var typeSymbols = workingTypeSymbols.ToSymbolSet(type => type.Symbol);
			var scopes = EnumerableExtensions.Concat(
				workingGvlSymbols.Cast<IScopeSymbol>(),
				typeSymbols.OfType<IScopeSymbol>()).ToSymbolSetWithDuplicates(messageBag);
			var functionSymbolSet = workingFunctionSymbols.ToSymbolSet(w => w.VariableSymbol);

			var itf = new BoundModuleInterface(
				typeSymbols,
				functionSymbolSet,
				workingGvlSymbols,
				scopes);

			var moduleScope = new GlobalModuleScope(itf, rootScope);
			var functionSymbols = workingFunctionSymbols
				.ToImmutableDictionary(
					w => w.VariableSymbol,
					w => w.GetBoundPou(moduleScope),
					SymbolByNameComparer<FunctionVariableSymbol>.Instance);
			var functionBlockSymbols = workingTypeSymbols
				.OfType<FunctionBlockTypeInWork>()
				.ToImmutableDictionary(
					w => w.Symbol,
					w => w.GetBoundPou(moduleScope),
					SymbolByNameComparer<FunctionBlockSymbol>.Instance);

			return new BoundModule(
				messageBag.ToImmutable(),
				itf,
				functionSymbols,
				functionBlockSymbols);
		}

		private static void CompleteSymbols(
			IScope scope,
			MessageBag messageBag,
			SymbolSet<ITypeSymbolInWork> typesToComplete,
			SymbolSet<GlobalVariableListSymbol> gvlsToComplete,
			SymbolSet<FunctionSymbolInWork> functionsToComplete)

		{
			// Complete the symbolic layout for all types.
			List<ITypeSymbol> typeSymbols = new List<ITypeSymbol>();
			foreach (var workingSymbol in typesToComplete)
				typeSymbols.Add(workingSymbol.CompleteSymbolic(scope, messageBag));

			// Perform the layout for all symbols.
			foreach (var typeSymbol in typeSymbols)
			{
				DelayedLayoutType.RecursiveLayout(typeSymbol, messageBag, typeSymbol.DeclaringPosition);

				if (typeSymbol is FunctionBlockSymbol fbSymbol)
				{
					foreach (var param in fbSymbol.Parameters)
					{
						DelayedLayoutType.RecursiveLayout(param.Type, messageBag, param.DeclaringPosition);
					}
				}
				else if (typeSymbol is EnumTypeSymbol enumTypeSymbol)
				{
					enumTypeSymbol.RecursiveInitializers(messageBag);
				}
			}
			foreach (var gvlSymbol in gvlsToComplete)
			{
				foreach (var globalVar in gvlSymbol.Variables)
					DelayedLayoutType.RecursiveLayout(globalVar.Type, messageBag, globalVar.DeclaringPosition);
			}
			foreach (var functionSymbol in functionsToComplete)
			{
				foreach (var param in functionSymbol.Symbol.Parameters)
					DelayedLayoutType.RecursiveLayout(param.Type, messageBag, param.DeclaringPosition);
			}
		}


		public static IEnumerable<TVariableSymbol> BindVariableBlocks<TVariableSymbol, TKind>(
			SyntaxArray<VarDeclBlockSyntax> vardecls,
			IScope scope,
			MessageBag messages,
			Func<IVarDeclKindToken, TKind?> kindSelector,
			Func<TKind, IScope, MessageBag, VarDeclSyntax, TVariableSymbol> varMap)
		{
			var result = new List<TVariableSymbol>();
			foreach (var block in vardecls)
			{
				var maybeKind = kindSelector(block.TokenKind);
				if (maybeKind is TKind kind)
				{
					foreach (var decl in block.Declarations)
					{
						var varSymbol = varMap(kind, scope, messages, decl);
						result.Add(varSymbol);
					}
				}
			}
			return result;
		}

		private static OrderedSymbolSet<ParameterVariableSymbol> BindParameters(IScope Scope, MessageBag Messages, PouInterfaceSyntax context)
		{
			var allParameters = BindParameters(Scope, Messages, context.VariableDeclarations).Concat(BindReturnValue(Scope, Messages, context.Name, context.ReturnDeclaration));
			var uniqueParameters = allParameters.ToOrderedSymbolSetWithDuplicates(Messages);
			return uniqueParameters;

			static IEnumerable<ParameterVariableSymbol> BindParameters(IScope Scope, MessageBag Messages, SyntaxArray<VarDeclBlockSyntax> vardecls)
				=> BindVariableBlocks(vardecls, Scope, Messages,
					kind => ParameterKind.TryMapDecl(kind),
					(kind, scope, bag, syntax) =>
					{
						IType type = TypeCompiler.MapSymbolic(Scope, syntax.Type, Messages);
						return new ParameterVariableSymbol(
							kind,
							syntax.TokenIdentifier.SourcePosition,
							syntax.Identifier,
							type);
					});
			static IEnumerable<ParameterVariableSymbol> BindReturnValue(IScope Scope, MessageBag Messages, CaseInsensitiveString functionName, ReturnDeclSyntax? syntax)
			{
				if (syntax != null)
				{
					IType type = TypeCompiler.MapSymbolic(Scope, syntax.Type, Messages);
					yield return new ParameterVariableSymbol(ParameterKind.Output, syntax.Type.SourcePosition, functionName, type);
				}
			}
		}
	}
}
