﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

		public BoundModule(
			ImmutableArray<IMessage> interfaceMessages,
			BoundModuleInterface @interface,
			ImmutableDictionary<FunctionSymbol, BoundPou> pous)
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
		public readonly SymbolSet<GlobalVariableListSymbol> GlobalVariableListSymbols;

		public BoundModuleInterface(SymbolSet<ITypeSymbol> dutTypes, SymbolSet<FunctionSymbol> functionSymbols, SymbolSet<GlobalVariableListSymbol> globalVariableListSymbols)
		{
			DutTypes = dutTypes;
			FunctionSymbols = functionSymbols;
			GlobalVariableListSymbols = globalVariableListSymbols;
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
			var localVariables = BindLocalVariables(Interface.VariableDeclarations, Scope, messageBag).ToSymbolSetWithDuplicates(messageBag);
			foreach (var local in localVariables)
			{
				if (Symbol.Parameters.TryGetValue(local.Name, out var existing))
					messageBag.Add(new SymbolAlreadyExistsMessage(local.Name, existing.DeclaringPosition, local.DeclaringPosition));
			}
			var scope = new InsideFunctionScope(Scope, Symbol, localVariables);
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
				syntax.TokenIdentifier.SourcePosition,
				syntax.Identifier,
				type);
		}
	}

	public sealed class ProjectBinder : AInnerScope<RootScope>
	{
		private readonly SymbolSet<ITypeSymbolInWork> WorkingTypeSymbols;
		private readonly SymbolSet<GlobalVariableListSymbol> WorkingGvlSymbols;
		private readonly MessageBag MessageBag = new();
		private readonly PouSymbolCreatorT PouSymbolCreator;

		private ProjectBinder(
			RootScope rootScope,
			ImmutableArray<ParsedDutLanguageSource> duts,
			ImmutableArray<ParsedGVLLanguageSource> gvls) : base(rootScope)
		{
			PouSymbolCreator = new(this, MessageBag);
			WorkingTypeSymbols = duts.ToSymbolSetWithDuplicates(MessageBag,
				x => x.Syntax.TypeBody.Accept(DutSymbolCreator.Instance, x.Syntax));
			WorkingGvlSymbols = gvls.ToSymbolSetWithDuplicates(MessageBag,
				x => GvlSymbolInWork.Create(x, this, MessageBag));
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
				var allParameters = BindParameters(context.VariableDeclarations).Concat(BindReturnValue(context.Name, context.ReturnDeclaration));
				var uniqueParameters = allParameters.ToOrderedSymbolSetWithDuplicates(Messages);
				return new FunctionSymbol(
					isProgram,
					context.Name,
					context.TokenName.SourcePosition,
					uniqueParameters);
			}

			private IEnumerable<ParameterSymbol> BindParameters(IEnumerable<VarDeclBlockSyntax> vardecls)
				=> vardecls.SelectMany(vardeclBlock => BindVarDeclBlock(vardeclBlock.TokenKind, vardeclBlock.Declarations));
			private IEnumerable<ParameterSymbol> BindVarDeclBlock(IVarDeclKindToken kind, SyntaxArray<VarDeclSyntax> vardecls)
			{
				var mapped = ParameterKind.TryMapDecl(kind);
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
					syntax.TokenIdentifier.SourcePosition,
					syntax.Identifier,
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
								new VariableExpressionSyntax(IdentifierToken.SynthesizeEx(0, prevSymbol.Name)),
								PlusToken.Synthesize(0),
								new LiteralExpressionSyntax(IntegerLiteralToken.SynthesizeEx(0, OverflowingInteger.FromUlong(1, false))));
						}
					}
					var valueSymbol = new EnumValueSymbol(innerScope, valueSyntax.SourcePosition, valueSyntax.Identifier, value, Symbol);
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

		public static BoundModule Bind(
			ImmutableArray<ParsedTopLevelInterfaceAndBodyPouLanguageSource> pous,
			ImmutableArray<ParsedGVLLanguageSource> gvls,
			ImmutableArray<ParsedDutLanguageSource> duts)
		{
			var binder = new ProjectBinder(new RootScope(new SystemScope()), duts, gvls);
			return binder.Bind(pous);
		}

		private BoundModule Bind(ImmutableArray<ParsedTopLevelInterfaceAndBodyPouLanguageSource> pous)
		{
			var typeSymbols = WorkingTypeSymbols.ToSymbolSet(symbolInWork => symbolInWork.CompleteSymbolic(this));
			foreach (var typeSymbol in typeSymbols)
				DelayedLayoutType.RecursiveLayout(typeSymbol, MessageBag, typeSymbol.DeclaringPosition);
			foreach (var enumTypeSymbol in typeSymbols.OfType<EnumTypeSymbol>())
				enumTypeSymbol.RecursiveInitializers(MessageBag);
			foreach (var gvlSymbol in WorkingGvlSymbols)
			{
				foreach(var globalVar in gvlSymbol.Variables)
					DelayedLayoutType.RecursiveLayout(globalVar.Type, MessageBag, globalVar.DeclaringPosition);
			}

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

			var itf = new BoundModuleInterface(typeSymbols, functionSymbols, WorkingGvlSymbols);
			return new BoundModule(
				MessageBag.ToImmutable(),
				itf,
				dictionary.ToImmutable());
		}

		public override ErrorsAnd<ITypeSymbol> LookupType(CaseInsensitiveString identifier, SourcePosition sourcePosition) =>
			WorkingTypeSymbols.TryGetValue(identifier, out var symbolInWork)
				? ErrorsAnd.Create(symbolInWork.Symbol)
				: base.LookupType(identifier, sourcePosition);
		public override ErrorsAnd<GlobalVariableListSymbol> LookupGlobalVariableList(CaseInsensitiveString identifier, SourcePosition sourcePosition) =>
			WorkingGvlSymbols.TryGetValue(identifier, out var symbol)
				? ErrorsAnd.Create(symbol)
				: base.LookupGlobalVariableList(identifier, sourcePosition);
	}
}
