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

		private ImmutableArray<IMessage>? _backingBindMessages;
		public IEnumerable<IMessage> BindMessages
		{
			get
			{
				if (!_backingBindMessages.HasValue)
				{
					var messages = ImmutableArray.CreateBuilder<IMessage>();
					foreach (var func in FunctionPous)
					{
						messages.AddRange(func.Value.BoundBody.Errors);
						messages.AddRange(func.Value.FlowAnalysis);
					}
					foreach (var fb in FunctionBlockPous)
					{
						messages.AddRange(fb.Value.BoundBody.Errors);
						messages.AddRange(fb.Value.FlowAnalysis);
					}
					_backingBindMessages = messages.ToImmutable();
				}
				return _backingBindMessages.Value;
			}
		}
	}

	public sealed class BoundModuleInterface
	{
		public readonly CaseInsensitiveString Name;
		public readonly SystemScope SystemScope;
		public readonly SymbolSet<ITypeSymbol> Types;
		public readonly SymbolSet<FunctionVariableSymbol> FunctionSymbols;
		public readonly SymbolSet<GlobalVariableListSymbol> GlobalVariableListSymbols;
		public readonly SymbolSet<LibrarySymbol> Libraries;

		public readonly SymbolSet<IScopeSymbol> Scopes;

		public BoundModuleInterface(
			CaseInsensitiveString name,
			SystemScope systemScope,
			SymbolSet<ITypeSymbol> types,
			SymbolSet<FunctionVariableSymbol> functionSymbols,
			SymbolSet<GlobalVariableListSymbol> globalVariableListSymbols,
			SymbolSet<LibrarySymbol> libraries,
			SymbolSet<IScopeSymbol> scopes)
		{
			Name = name;
			SystemScope = systemScope;
			Types = types;
			FunctionSymbols = functionSymbols;
			GlobalVariableListSymbols = globalVariableListSymbols;
			Libraries = libraries;
			Scopes = scopes;
		}
	}

	public abstract class BoundPou
	{
		public readonly ICallableTypeSymbol CallableSymbol;
		protected abstract (ErrorsAnd<IBoundStatement>, RootVarDeclTreeNode) BoundBodyAndTemps { get; }
		private ImmutableArray<IMessage>? _lazyFlowAnalyis;
		public ImmutableArray<IMessage> FlowAnalysis
		{
			get
			{
				if (!_lazyFlowAnalyis.HasValue)
				{
					_lazyFlowAnalyis = AnalyzeFlow(CallableSymbol, BoundBodyAndTemps);
				}
				return _lazyFlowAnalyis.Value;
			}
		}

		public OrderedSymbolSet<LocalVariableSymbol> LocalVariables => BoundBodyAndTemps.Item2.Locals;
		public ErrorsAnd<IBoundStatement> BoundBody => BoundBodyAndTemps.Item1;


		private BoundPou(ICallableTypeSymbol callableSymbol)
		{
			CallableSymbol = callableSymbol ?? throw new ArgumentNullException(nameof(callableSymbol));
		}

		private sealed class BoundFunction : BoundPou
		{
			private readonly Lazy<(ErrorsAnd<IBoundStatement>, RootVarDeclTreeNode)> _lazyBoundBody;

			public BoundFunction(
				FunctionTypeSymbol symbol,
				Lazy<(ErrorsAnd<IBoundStatement>, RootVarDeclTreeNode)> lazyBoundBody) : base(symbol)
			{
				_lazyBoundBody = lazyBoundBody;
			}
			protected override (ErrorsAnd<IBoundStatement>, RootVarDeclTreeNode) BoundBodyAndTemps => _lazyBoundBody.Value;

			public static BoundFunction Create(
				IScope scope, PouInterfaceSyntax @interface, StatementListSyntax body, FunctionTypeSymbol symbol)
			{
				(ErrorsAnd<IBoundStatement>, RootVarDeclTreeNode) Bind()
				{
					var messageBag = new MessageBag();
					var callableScope = new InsideCallableScope(scope, symbol);
					var rootNode = RootVarDeclTreeNode.FromLocalsSyntax(@interface.VariableDeclarations, callableScope);
					Func<IVariableSymbol, IVariableSymbol?> getAlreadyDeclared = local => symbol.Parameters.TryGetValue(local.Name);
					return CreateBoundBody(callableScope, rootNode, messageBag, body, getAlreadyDeclared);
				}

				var lazyBound = LazyExtensions.Create(Bind);
				return new BoundFunction(symbol, lazyBound);
			}
		}
		private sealed class BoundFunctionBlock : BoundPou
		{
			private readonly Lazy<(ErrorsAnd<IBoundStatement>, RootVarDeclTreeNode)> _lazyBoundBody;

			public BoundFunctionBlock(
				FunctionBlockSymbol symbol,
				Lazy<(ErrorsAnd<IBoundStatement>, RootVarDeclTreeNode)> lazyBoundBody) :
				base(symbol)
			{
				_lazyBoundBody = lazyBoundBody;
			}

			protected override (ErrorsAnd<IBoundStatement>, RootVarDeclTreeNode) BoundBodyAndTemps => _lazyBoundBody.Value;

			public static BoundFunctionBlock Create(IScope scope, PouInterfaceSyntax @interface, StatementListSyntax body, FunctionBlockSymbol symbol)
			{
				(ErrorsAnd<IBoundStatement>, RootVarDeclTreeNode) Bind()
				{
					var messageBag = new MessageBag();
					var insideFbScope = new InsideTypeScope(new InsideCallableScope(scope, symbol), symbol.Fields);
					var rootNode = RootVarDeclTreeNode.FromLocalsSyntax(@interface.VariableDeclarations, insideFbScope);
					Func<IVariableSymbol, IVariableSymbol?> getAlreadyDeclared = local => (IVariableSymbol?)symbol.Parameters.TryGetValue(local.Name) ?? symbol.Fields.TryGetValue(local.Name);
					return CreateBoundBody(insideFbScope, rootNode, messageBag, body, getAlreadyDeclared);
				}

				var lazyBound = LazyExtensions.Create(Bind);
				return new BoundFunctionBlock(symbol, lazyBound);
			}
		}

		static (ErrorsAnd<IBoundStatement>, RootVarDeclTreeNode) CreateBoundBody(
			IScope outerScope,
			RootVarDeclTreeNode rootNode,
			MessageBag messageBag,
			StatementListSyntax body,
			Func<IVariableSymbol, IVariableSymbol?> getAlreadyDeclared)
		{
			var scope = rootNode.GetScope(outerScope);
			var bound = StatementBinder.Bind(body, rootNode, scope, messageBag);
			rootNode.GetAllMessages(messageBag, getAlreadyDeclared);
			return (messageBag.ToErrorsAnd(bound), rootNode);
		}
		private static ImmutableArray<IMessage> AnalyzeFlow(ICallableTypeSymbol symbol,
			(ErrorsAnd<IBoundStatement>, RootVarDeclTreeNode) bound)
		{
			var (boundBody, rootVarDeclNode) = bound;
			var trackedVariables = GetTrackedVariables(symbol, rootVarDeclNode);

			var messageBag = new MessageBag();
			FlowAnalyzer.Analyse(rootVarDeclNode.Locals, boundBody.Value, trackedVariables, messageBag);
			return messageBag.ToImmutable();
		}

		private static ImmutableArray<FlowAnalyzer.TrackedVariable> GetTrackedVariables(ICallableTypeSymbol symbol, RootVarDeclTreeNode rootVarDeclNode)
		{
			var trackedVariables = ImmutableArray.CreateBuilder<FlowAnalyzer.TrackedVariable>();
			rootVarDeclNode.GetAllTrackedVariables(trackedVariables);
			foreach (var p in symbol.Parameters)
			{
				if (p.Kind == ParameterKind.Output)
					trackedVariables.Add(FlowAnalyzer.TrackedVariable.Required(p));
			}
			return trackedVariables.ToImmutable();
		}

		public static BoundPou FromFunction(IScope scope, PouInterfaceSyntax @interface, StatementListSyntax body, FunctionTypeSymbol symbol)
			=> BoundFunction.Create(scope, @interface, body, symbol);

		public static BoundPou FromFunctionBlock(IScope scope, PouInterfaceSyntax @interface, StatementListSyntax body, FunctionBlockSymbol symbol)
			=> BoundFunctionBlock.Create(scope, @interface, body, symbol);
	}

	public sealed class LibrarySymbol : IScopeSymbol
	{
		public CaseInsensitiveString Name { get; }
		public SourceSpan DeclaringSpan { get; }
		public readonly BoundModuleInterface Interface;

		public LibrarySymbol(CaseInsensitiveString name, SourceSpan declaringSpan, BoundModuleInterface @interface)
		{
			Name = name;
			DeclaringSpan = declaringSpan;
			Interface = @interface ?? throw new ArgumentNullException(nameof(@interface));
		}

		public ErrorsAnd<IScopeSymbol> LookupScope(CaseInsensitiveString identifier, SourceSpan errorPosition)
			=> Interface.Scopes.TryGetValue(identifier, out var value) && value is not LibrarySymbol // Libraries are not accessable from the outside.
			? ErrorsAnd.Create(value)
			: EmptyScopeHelper.LookupScope(Name, identifier, errorPosition);

		public ErrorsAnd<ITypeSymbol> LookupType(CaseInsensitiveString identifier, SourceSpan errorPosition)
			=> Interface.Types.TryGetValue(identifier, out var value)
			? ErrorsAnd.Create(value)
			: EmptyScopeHelper.LookupType(Name, identifier, errorPosition);
		public ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourceSpan errorPosition)
			=> Interface.FunctionSymbols.TryGetValue(identifier, out var value)
			? ErrorsAnd.Create<IVariableSymbol>(value)
			: EmptyScopeHelper.LookupVariable(Name, identifier, errorPosition);

		public override string ToString() => Name.ToString();
	}

	public static class ProjectBinder
	{
		private sealed class Scope : AInnerScope<RootScope>
		{
			public readonly SymbolSet<LibrarySymbol> Libraries;
			public readonly SymbolSet<ITypeSymbol> TypeSymbols;
			public readonly SymbolSet<GlobalVariableListSymbol> GvlSymbols;
			public readonly SymbolSet<FunctionVariableSymbol> WorkingFunctionSymbols;

			public Scope(
				SymbolSet<LibrarySymbol> librarySymbols,
				SymbolSet<ITypeSymbol> workingTypeSymbols,
				SymbolSet<GlobalVariableListSymbol> workingGvlSymbols,
				SymbolSet<FunctionVariableSymbol> workingFunctionSymbols,
				RootScope outerScope) : base(outerScope)
			{
				Libraries = librarySymbols;
				TypeSymbols = workingTypeSymbols;
				GvlSymbols = workingGvlSymbols;
				WorkingFunctionSymbols = workingFunctionSymbols;
			}

			public override ErrorsAnd<ITypeSymbol> LookupType(CaseInsensitiveString identifier, SourceSpan sourceSpan) =>
				TypeSymbols.TryGetValue(identifier, out var symbolInWork)
					? ErrorsAnd.Create(symbolInWork)
					: base.LookupType(identifier, sourceSpan);
			public override ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourceSpan sourceSpan) =>
				WorkingFunctionSymbols.TryGetValue(identifier, out var symbolInWork)
					? ErrorsAnd.Create<IVariableSymbol>(symbolInWork)
					: base.LookupVariable(identifier, sourceSpan);
			public override ErrorsAnd<IScopeSymbol> LookupScope(CaseInsensitiveString identifier, SourceSpan sourceSpan) =>
				Libraries.TryGetValue(identifier, out var library)
				? ErrorsAnd.Create<IScopeSymbol>(library)
				: (
				GvlSymbols.TryGetValue(identifier, out var symbol)
					? ErrorsAnd.Create<IScopeSymbol>(symbol)
					: base.LookupScope(identifier, sourceSpan));
		}

		private sealed class DutSymbolCreator : ITypeDeclarationBodySyntax.IVisitor<ITypeSymbolInWork, DutSymbolCreator.Context>
		{
			public readonly struct Context
			{
				public readonly TypeDeclarationSyntax Syntax;
				public readonly CaseInsensitiveString ModuleName;

				public Context(TypeDeclarationSyntax syntax, CaseInsensitiveString moduleName)
				{
					Syntax = syntax;
					ModuleName = moduleName;
				}
			}

			public static readonly DutSymbolCreator Instance = new();
			public ITypeSymbolInWork Visit(AliasTypeDeclarationBodySyntax aliasTypeDeclarationBodySyntax, Context context)
				=> new AliasTypeInWork(context.Syntax.TokenIdentifier.SourceSpan, context.ModuleName, context.Syntax.Identifier, aliasTypeDeclarationBodySyntax.BaseType);
			public ITypeSymbolInWork Visit(StructTypeDeclarationBodySyntax structTypeDeclarationBodySyntax, Context context)
				=> new StructuredTypeInWork(context.Syntax.TokenIdentifier.SourceSpan, context.ModuleName, context.Syntax.Identifier, false, structTypeDeclarationBodySyntax.Fields);
			public ITypeSymbolInWork Visit(UnionTypeDeclarationBodySyntax unionTypeDeclarationBodySyntax, Context context)
				=> new StructuredTypeInWork(context.Syntax.TokenIdentifier.SourceSpan, context.ModuleName, context.Syntax.Identifier, true, unionTypeDeclarationBodySyntax.Fields);
			public ITypeSymbolInWork Visit(EnumTypeDeclarationBodySyntax enumTypeDeclarationBodySyntax, Context context)
				=> new EnumTypeInWork(context.Syntax.TokenIdentifier.SourceSpan, context.ModuleName, context.Syntax.Identifier, enumTypeDeclarationBodySyntax);
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

			public FunctionTypeSymbol? Visit(FunctionToken functionToken, PouInterfaceSyntax context)
			{
				OrderedSymbolSet<ParameterVariableSymbol> uniqueParameters = BindParameters(Scope, Messages, context);
				return new FunctionTypeSymbol(
					Scope.SystemScope.ModuleName,
					context.Name,
					context.TokenName.SourceSpan,
					uniqueParameters);
			}

			public FunctionTypeSymbol? Visit(FunctionBlockToken functionBlockToken, PouInterfaceSyntax context) => null;
		}

		private sealed class PouTypeSymbolCreator : IPouKindToken.IVisitor<ITypeSymbolInWork?, PouTypeSymbolCreator.Context>
		{
			public readonly struct Context
			{
				public readonly ParsedTopLevelInterfaceAndBodyPouLanguageSource Syntax;
				public readonly CaseInsensitiveString ModuleName;

				public Context(ParsedTopLevelInterfaceAndBodyPouLanguageSource syntax, CaseInsensitiveString moduleName)
				{
					Syntax = syntax;
					ModuleName = moduleName;
				}
			}

			public static readonly PouTypeSymbolCreator Instance = new();
			public ITypeSymbolInWork? Visit(FunctionToken functionToken, Context context) => null;
			public ITypeSymbolInWork? Visit(FunctionBlockToken functionBlockToken, Context context) =>
				new FunctionBlockTypeInWork(context.ModuleName, context.Syntax.Interface, context.Syntax.Body);
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

			public StructuredTypeInWork(
				SourceSpan declaringSpan,
				CaseInsensitiveString moduleName,
				CaseInsensitiveString name,
				bool isUnion,
				SyntaxArray<VarDeclSyntax> fields)
			{
				Symbol = new StructuredTypeSymbol(declaringSpan, isUnion, moduleName, name);
				FieldsSyntax = fields;
			}

			ITypeSymbol ITypeSymbolInWork.Symbol => Symbol;
			public CaseInsensitiveString Name => Symbol.Name;
			public SourceSpan DeclaringSpan => Symbol.DeclaringSpan;

			public ITypeSymbol CompleteSymbolic(IScope scope, MessageBag messageBag)
			{
				var fieldSymbols = FieldsSyntax.ToSymbolSetWithDuplicates(messageBag, x => CreateFieldSymbol(scope, messageBag, x));
				Symbol.InternalSetFields(fieldSymbols);
				return Symbol;
			}

			private static FieldVariableSymbol CreateFieldSymbol(IScope scope, MessageBag messageBag, VarDeclSyntax fieldSyntax)
			{
				if (fieldSyntax.Initial != null)
					messageBag.Add(new VariableCannotHaveInitialValueMessage(fieldSyntax.Initial.SourceSpan));
				var typeSymbol = TypeCompiler.MapSymbolic(scope, fieldSyntax.Type.Type, messageBag);
				return new FieldVariableSymbol(fieldSyntax.SourceSpan, fieldSyntax.Identifier, typeSymbol);
			}
		}

		private sealed class EnumTypeInWork : ITypeSymbolInWork
		{
			private readonly EnumTypeSymbol Symbol;
			private readonly EnumTypeDeclarationBodySyntax BodySyntax;

			public EnumTypeInWork(SourceSpan declaringSpan, CaseInsensitiveString moduleName, CaseInsensitiveString name, EnumTypeDeclarationBodySyntax bodySyntax)
			{
				Symbol = new EnumTypeSymbol(declaringSpan, moduleName, name);
				BodySyntax = bodySyntax ?? throw new ArgumentNullException(nameof(bodySyntax));
			}

			ITypeSymbol ITypeSymbolInWork.Symbol => Symbol;
			public CaseInsensitiveString Name => Symbol.Name;
			public SourceSpan DeclaringSpan => Symbol.DeclaringSpan;

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
							value = new LiteralExpressionSyntax(IntegerLiteralToken.SynthesizeEx(SourcePoint.Null, OverflowingInteger.FromUlong(0, false)));
						}
						else
						{
							value = new BinaryOperatorExpressionSyntax(
								new VariableExpressionSyntax(IdentifierToken.SynthesizeEx(SourcePoint.Null, prevSymbol.Name)),
								PlusToken.Synthesize(SourcePoint.Null),
								new LiteralExpressionSyntax(IntegerLiteralToken.SynthesizeEx(SourcePoint.Null, OverflowingInteger.FromUlong(1, false))));
						}
					}
					var valueSymbol = new EnumVariableSymbol(innerScope, valueSyntax.SourceSpan, valueSyntax.Identifier, value, Symbol);
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

			public AliasTypeInWork(SourceSpan declaringSpan, CaseInsensitiveString moduleName, CaseInsensitiveString name, ITypeSyntax aliasedTypeSyntax)
			{
				Symbol = new AliasTypeSymbol(declaringSpan, moduleName, name);
				AliasedTypeSyntax = aliasedTypeSyntax;
			}

			ITypeSymbol ITypeSymbolInWork.Symbol => Symbol;
			public CaseInsensitiveString Name => Symbol.Name;
			public SourceSpan DeclaringSpan => Symbol.DeclaringSpan;

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

			public FunctionBlockTypeInWork(CaseInsensitiveString moduleName, PouInterfaceSyntax syntax, StatementListSyntax body)
			{
				Syntax = syntax ?? throw new ArgumentNullException(nameof(syntax));
				BodySyntax = body ?? throw new ArgumentNullException(nameof(body));
				Symbol = new FunctionBlockSymbol(syntax.SourceSpan, moduleName, syntax.Name);
			}

			public FunctionBlockSymbol Symbol { get; }
			ITypeSymbol ITypeSymbolInWork.Symbol => Symbol;
			public CaseInsensitiveString Name => Symbol.Name;
			public SourceSpan DeclaringSpan => Symbol.DeclaringSpan;
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
						messageBag.Add(new SymbolAlreadyExistsMessage(field.Name, original.DeclaringSpan, field.DeclaringSpan));
					}
				}
				return Symbol;
			}
			public BoundPou GetBoundPou(IScope moduleScope)
				=> BoundPou.FromFunctionBlock(moduleScope, Syntax, BodySyntax, Symbol);
			private static IEnumerable<FieldVariableSymbol> BindFields(IScope scope, MessageBag messageBag, SyntaxArray<VarDeclBlockSyntax> vardecls)
				=> BindVariableBlocks(vardecls, scope, messageBag,
					kindToken => kindToken as VarInstToken,
					(_, scope, bag, syntax) =>
					{
						if (syntax.Initial != null)
							messageBag.Add(new VariableCannotHaveInitialValueMessage(syntax.Initial.SourceSpan));
						IType type = TypeCompiler.MapSymbolic(scope, syntax.Type.Type, bag);
						return new FieldVariableSymbol(
							syntax.TokenIdentifier.SourceSpan,
							syntax.Identifier,
							type);
					});

		}

		private static class GvlFactory
		{
			public static GlobalVariableListSymbol Create(ParsedGVLLanguageSource x, IScope scope, MessageBag messageBag)
			{
				var variables = BindVariables(x.Name, x.Syntax.VariableDeclarations, scope, messageBag).ToSymbolSetWithDuplicates(messageBag);
				return new GlobalVariableListSymbol(x.Syntax.SourceSpan, x.Name, variables);
			}
			private static IEnumerable<GlobalVariableSymbol> BindVariables(CaseInsensitiveString gvlName, IEnumerable<VarDeclBlockSyntax> vardecls, IScope scope, MessageBag messages)
				=> vardecls.SelectMany(vardeclBlock => BindVarDeclBlock(vardeclBlock.TokenKind, gvlName, vardeclBlock.Declarations, scope, messages));
			private static IEnumerable<GlobalVariableSymbol> BindVarDeclBlock(IVarDeclKindToken kind, CaseInsensitiveString gvlName, SyntaxArray<VarDeclSyntax> vardecls, IScope scope, MessageBag messages)
			{
				if (kind is not VarGlobalToken)
					messages.Add(new OnlyVarGlobalInGvlMessage(kind.SourceSpan));
				return vardecls.Select(v => BindVarDecl(gvlName, v, scope, messages));
			}
			private static GlobalVariableSymbol BindVarDecl(CaseInsensitiveString gvlName, VarDeclSyntax syntax, IScope scope, MessageBag messages)
			{
				IType type = TypeCompiler.MapSymbolic(scope, syntax.Type.Type, messages);
				return new(
					syntax.TokenIdentifier.SourceSpan,
					scope.SystemScope.ModuleName,
					gvlName,
					syntax.Identifier,
					type,
					syntax.Initial?.Value);
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
			public SourceSpan DeclaringSpan => ((ISymbol)Symbol).DeclaringSpan;

			public BoundPou GetBoundPou(IScope moduleScope)
				=> BoundPou.FromFunction(moduleScope, InterfaceSyntax, BodySyntax, Symbol);
		}

		public static BoundModule Bind(
			CaseInsensitiveString moduleName,
			ImmutableArray<ParsedTopLevelInterfaceAndBodyPouLanguageSource> pous,
			ImmutableArray<ParsedGVLLanguageSource> gvls,
			ImmutableArray<ParsedDutLanguageSource> duts,
			ImmutableArray<LibraryLanguageSource> libraries)
		{
			var systemScope = new SystemScope(moduleName);
			var rootScope = new RootScope(systemScope);
			var messageBag = new MessageBag();

			var librarySymbols = libraries.ToSymbolSetWithDuplicates(messageBag, lib => new LibrarySymbol(lib.Namespace, lib.SourceSpan, lib.Interface));
			var dutSymbols = ImmutableArray.CreateRange(duts, dut => dut.Syntax.TypeBody.Accept(DutSymbolCreator.Instance, new(dut.Syntax, moduleName)));
			var fbTypeSymbols = pous.Select(pou => pou.Interface.TokenPouKind.Accept(PouTypeSymbolCreator.Instance, new(pou, moduleName))).WhereNotNull().ToImmutableArray();
			var workingTypeSymbols = dutSymbols.Concat(fbTypeSymbols).ToSymbolSetWithDuplicates(messageBag);
			var typeScope = new Scope(
				librarySymbols,
				workingTypeSymbols.ToSymbolSet(t => t.Symbol),
				SymbolSet<GlobalVariableListSymbol>.Empty,
				SymbolSet<FunctionVariableSymbol>.Empty,
				rootScope);
			var workingGvlSymbols = gvls.ToSymbolSetWithDuplicates(messageBag,
				x => GvlFactory.Create(x, typeScope, messageBag));
			var typeFuncScope = new Scope(
				typeScope.Libraries,
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
				typeScope.Libraries,
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
				typeSymbols.OfType<IScopeSymbol>(),
				librarySymbols).ToSymbolSetWithDuplicates(messageBag);
			var functionSymbolSet = workingFunctionSymbols.ToSymbolSet(w => w.VariableSymbol);

			var itf = new BoundModuleInterface(
				moduleName,
				systemScope,
				typeSymbols,
				functionSymbolSet,
				workingGvlSymbols,
				librarySymbols,
				scopes);

			var moduleScope = new GlobalInternalModuleScope(itf, rootScope);
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
				DelayedLayoutType.RecursiveLayout(typeSymbol, messageBag, typeSymbol.DeclaringSpan);

				if (typeSymbol is FunctionBlockSymbol fbSymbol)
				{
					foreach (var param in fbSymbol.Parameters)
					{
						DelayedLayoutType.RecursiveLayout(param.Type, messageBag, param.DeclaringSpan);
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
					DelayedLayoutType.RecursiveLayout(globalVar.Type, messageBag, globalVar.DeclaringSpan);
			}
			foreach (var functionSymbol in functionsToComplete)
			{
				foreach (var param in functionSymbol.Symbol.Parameters)
					DelayedLayoutType.RecursiveLayout(param.Type, messageBag, param.DeclaringSpan);
			}

			// Initial values
			foreach (var gvlSymbol in gvlsToComplete)
			{
				foreach (var globalVar in gvlSymbol.Variables)
					globalVar._CompleteInitialValue(scope, messageBag);
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
			int paramId = 0;
			var allParameters = BindParameters(Scope, Messages, context.VariableDeclarations).Concat(BindReturnValue(Scope, Messages, context.Name, context.ReturnDeclaration));
			var uniqueParameters = allParameters.ToOrderedSymbolSetWithDuplicates(Messages);
			return uniqueParameters;

			IEnumerable<ParameterVariableSymbol> BindParameters(IScope scope, MessageBag messages, SyntaxArray<VarDeclBlockSyntax> vardecls)
				=> BindVariableBlocks(vardecls, scope, messages,
					kind => ParameterKind.TryMapDecl(kind),
					(kind, scope, bag, syntax) =>
					{
						if (syntax.Initial != null)
							messages.Add(new VariableCannotHaveInitialValueMessage(syntax.Initial.SourceSpan));
						IType type = TypeCompiler.MapSymbolic(scope, syntax.Type.Type, messages);
						return new ParameterVariableSymbol(
							kind,
							syntax.TokenIdentifier.SourceSpan,
							syntax.Identifier,
							paramId++,
							type);
					});
			IEnumerable<ParameterVariableSymbol> BindReturnValue(IScope scope, MessageBag messages, CaseInsensitiveString functionName, VarTypeSyntax? syntax)
			{
				if (syntax != null)
				{
					IType type = TypeCompiler.MapSymbolic(scope, syntax.Type, messages);
					yield return new ParameterVariableSymbol(ParameterKind.Output, syntax.Type.SourceSpan, functionName, ++paramId, type);
				}
			}
		}
	}
}
