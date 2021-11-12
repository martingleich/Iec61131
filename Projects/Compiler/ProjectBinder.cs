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
		public readonly ImmutableDictionary<FunctionSymbol, BoundPou> FunctionPous;
		public readonly ImmutableDictionary<FunctionBlockSymbol, BoundPou> FunctionBlockPous;

		public BoundModule(
			ImmutableArray<IMessage> interfaceMessages,
			BoundModuleInterface @interface,
			ImmutableDictionary<FunctionSymbol, BoundPou> functionPous,
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
		public readonly SymbolSet<FunctionSymbol> FunctionSymbols;
		public readonly SymbolSet<GlobalVariableListSymbol> GlobalVariableListSymbols;

		public BoundModuleInterface(SymbolSet<ITypeSymbol> types, SymbolSet<FunctionSymbol> functionSymbols, SymbolSet<GlobalVariableListSymbol> globalVariableListSymbols)
		{
			Types = types;
			FunctionSymbols = functionSymbols;
			GlobalVariableListSymbols = globalVariableListSymbols;
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
		public static BoundPou FromFunction(IScope scope, PouInterfaceSyntax @interface, StatementListSyntax body, FunctionSymbol symbol)
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

	public sealed class ProjectBinder : AInnerScope<RootScope>
	{
		private readonly SymbolSet<ITypeSymbolInWork> WorkingTypeSymbols;
		private readonly SymbolSet<GlobalVariableListSymbol> WorkingGvlSymbols;
		private readonly SymbolSet<FunctionSymbolInWork> WorkingFunctionSymbols;
		private readonly MessageBag MessageBag = new();

		private ProjectBinder(
			RootScope rootScope,
			ImmutableArray<ParsedDutLanguageSource> duts,
			ImmutableArray<ParsedGVLLanguageSource> gvls,
			ImmutableArray<ParsedTopLevelInterfaceAndBodyPouLanguageSource> pous) : base(rootScope)
		{
			var dutSymbols = ImmutableArray.CreateRange(duts, dut => dut.Syntax.TypeBody.Accept(DutSymbolCreator.Instance, dut.Syntax));
			var fbTypeSymbols = pous.Select(pou => pou.Interface.TokenPouKind.Accept(PouTypeSymbolCreatorT.Instance, pou)).WhereNotNull().ToImmutableArray();
			WorkingTypeSymbols = dutSymbols.Concat(fbTypeSymbols).ToSymbolSetWithDuplicates(MessageBag);
			WorkingGvlSymbols = gvls.ToSymbolSetWithDuplicates(MessageBag,
				x => GvlSymbolInWork.Create(x, this, MessageBag));
			var pouFunctionSymbolCreator = new PouFunctionSymbolCreatorT(this, MessageBag);
			WorkingFunctionSymbols = (from pou in pous
									  let symbol = pouFunctionSymbolCreator.ConvertToSymbol(pou.Interface)
									  where symbol != null
									  select new FunctionSymbolInWork(symbol, pou.Interface, pou.Body)).ToSymbolSetWithDuplicates(MessageBag);
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

		private sealed class PouFunctionSymbolCreatorT : IPouKindToken.IVisitor<FunctionSymbol?, PouInterfaceSyntax>
		{
			private readonly IScope Scope;
			private readonly MessageBag Messages;

			public PouFunctionSymbolCreatorT(IScope scope, MessageBag messages)
			{
				Scope = scope ?? throw new ArgumentNullException(nameof(scope));
				Messages = messages ?? throw new ArgumentNullException(nameof(messages));
			}

			public FunctionSymbol? ConvertToSymbol(PouInterfaceSyntax syntax) => syntax.TokenPouKind.Accept(this, syntax);

			public FunctionSymbol? Visit(ProgramToken programToken, PouInterfaceSyntax context)
				=> TypifyFunctionOrProgram(isProgram: true, context);
			public FunctionSymbol? Visit(FunctionToken functionToken, PouInterfaceSyntax context)
				=> TypifyFunctionOrProgram(isProgram: false, context);
			public FunctionSymbol? Visit(FunctionBlockToken functionBlockToken, PouInterfaceSyntax context) => null;

			private FunctionSymbol TypifyFunctionOrProgram(bool isProgram, PouInterfaceSyntax context)
			{
				OrderedSymbolSet<ParameterVariableSymbol> uniqueParameters = BindParameters(Scope, Messages, context);
				return new FunctionSymbol(
					isProgram,
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
				Symbol.InternalSetFields(fieldSymbols);
				return Symbol;
			}

			private static FieldVariableSymbol CreateFieldSymbol(ProjectBinder projectBinder, VarDeclSyntax fieldSyntax)
			{
				var typeSymbol = TypeCompiler.MapSymbolic(projectBinder, fieldSyntax.Type, projectBinder.MessageBag);
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

			public ITypeSymbol CompleteSymbolic(ProjectBinder projectBinder)
			{
				var baseType = BodySyntax.EnumBaseType != null
					? TypeCompiler.MapSymbolic(projectBinder, BodySyntax.EnumBaseType, projectBinder.MessageBag)
					: projectBinder.SystemScope.Int;
				Symbol._SetBaseType(baseType);
				List<EnumVariableSymbol> allValueSymbols = new List<EnumVariableSymbol>();
				EnumVariableSymbol? prevSymbol = null;
				var innerScope = new InsideEnumScope(Symbol, projectBinder);
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
				var uniqueValueSymbols = allValueSymbols.ToSymbolSetWithDuplicates(projectBinder.MessageBag);
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

			public ITypeSymbol CompleteSymbolic(ProjectBinder projectBinder)
			{
				Symbol._SetAliasedType(TypeCompiler.MapSymbolic(projectBinder, AliasedTypeSyntax, projectBinder.MessageBag));
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
			public ITypeSymbol CompleteSymbolic(ProjectBinder projectBinder)
			{
				var parameters = BindParameters(projectBinder, projectBinder.MessageBag, Syntax);
				Symbol._SetParameters(parameters);
				var fields = BindFields(projectBinder, Syntax.VariableDeclarations).ToSymbolSetWithDuplicates(projectBinder.MessageBag);
				Symbol._SetFields(fields);
				foreach (var field in fields)
				{
					if (parameters.TryGetValue(field.Name, out var original))
					{
						projectBinder.MessageBag.Add(new SymbolAlreadyExistsMessage(field.Name, original.DeclaringPosition, field.DeclaringPosition));
					}
				}
				return Symbol;
			}
			public BoundPou GetBoundPou(IScope moduleScope)
				=> BoundPou.FromFunctionBlock(moduleScope, Syntax, BodySyntax, Symbol);
			private static IEnumerable<FieldVariableSymbol> BindFields(ProjectBinder projectBinder, SyntaxArray<VarDeclBlockSyntax> vardecls)
				=> BindVariableBlocks(vardecls, projectBinder, projectBinder.MessageBag,
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
			public static GlobalVariableListSymbol Create(ParsedGVLLanguageSource x, ProjectBinder projectBinder, MessageBag messageBag)
			{
				var variables = BindVariables(x.Syntax.VariableDeclarations, projectBinder, messageBag).ToSymbolSetWithDuplicates(messageBag);
				var symbol = new GlobalVariableListSymbol(x.Syntax.SourcePosition, x.Name, variables);
				return symbol;
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
			public FunctionSymbolInWork(FunctionSymbol symbol, PouInterfaceSyntax interfaceSyntax, StatementListSyntax bodySyntax)
			{
				Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
				InterfaceSyntax = interfaceSyntax ?? throw new ArgumentNullException(nameof(interfaceSyntax));
				BodySyntax = bodySyntax ?? throw new ArgumentNullException(nameof(bodySyntax));
			}
			public FunctionSymbol Symbol { get; }

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
			var binder = new ProjectBinder(new RootScope(new SystemScope()), duts, gvls, pous);
			return binder.Bind(pous);
		}

		private BoundModule Bind(ImmutableArray<ParsedTopLevelInterfaceAndBodyPouLanguageSource> pous)
		{
			var typeSymbols = WorkingTypeSymbols.ToSymbolSet(symbolInWork => symbolInWork.CompleteSymbolic(this));
			foreach (var typeSymbol in typeSymbols)
			{
				DelayedLayoutType.RecursiveLayout(typeSymbol, MessageBag, typeSymbol.DeclaringPosition);
				if (typeSymbol is FunctionBlockSymbol fbSymbol)
				{
					foreach (var param in fbSymbol.Parameters)
					{
						DelayedLayoutType.RecursiveLayout(param.Type, MessageBag, param.DeclaringPosition);
					}
				}
			}
			foreach (var enumTypeSymbol in typeSymbols.OfType<EnumTypeSymbol>())
				enumTypeSymbol.RecursiveInitializers(MessageBag);
			foreach (var gvlSymbol in WorkingGvlSymbols)
			{
				foreach (var globalVar in gvlSymbol.Variables)
					DelayedLayoutType.RecursiveLayout(globalVar.Type, MessageBag, globalVar.DeclaringPosition);
			}
			foreach (var functionSymbol in WorkingFunctionSymbols)
			{
				foreach (var param in functionSymbol.Symbol.Parameters)
					DelayedLayoutType.RecursiveLayout(param.Type, MessageBag, param.DeclaringPosition);
			}

			var itf = new BoundModuleInterface(
				typeSymbols,
				WorkingFunctionSymbols.ToSymbolSet(w => w.Symbol),
				WorkingGvlSymbols);

			var moduleScope = new GlobalModuleScope(itf, OuterScope);
			var functionSymbols = WorkingFunctionSymbols
				.ToImmutableDictionary(
					w => w.Symbol,
					w => w.GetBoundPou(moduleScope),
					SymbolByNameComparer<FunctionSymbol>.Instance);
			var functionBlockSymbols = WorkingTypeSymbols
				.OfType<FunctionBlockTypeInWork>()
				.ToImmutableDictionary(
					w => w.Symbol,
					w => w.GetBoundPou(moduleScope),
					SymbolByNameComparer<FunctionBlockSymbol>.Instance);

			return new BoundModule(
				MessageBag.ToImmutable(),
				itf,
				functionSymbols,
				functionBlockSymbols);
		}

		public override ErrorsAnd<ITypeSymbol> LookupType(CaseInsensitiveString identifier, SourcePosition sourcePosition) =>
			WorkingTypeSymbols.TryGetValue(identifier, out var symbolInWork)
				? ErrorsAnd.Create(symbolInWork.Symbol)
				: base.LookupType(identifier, sourcePosition);
		public override ErrorsAnd<GlobalVariableListSymbol> LookupGlobalVariableList(CaseInsensitiveString identifier, SourcePosition sourcePosition) =>
			WorkingGvlSymbols.TryGetValue(identifier, out var symbol)
				? ErrorsAnd.Create(symbol)
				: base.LookupGlobalVariableList(identifier, sourcePosition);

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
