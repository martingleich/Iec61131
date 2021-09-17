using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Compiler.Messages;
using Compiler.Types;

namespace Compiler
{
	public sealed class LazyBoundModule
	{
		public readonly ImmutableArray<IMessage> InterfaceMessages;
		public readonly BoundModuleInterface Interface;

		public LazyBoundModule(ImmutableArray<IMessage> interfaceMessages, BoundModuleInterface @interface)
		{
			InterfaceMessages = interfaceMessages;
			Interface = @interface;
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

	public sealed class ProjectBinder : AInnerScope
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
			{
				throw new NotImplementedException();
			}

			public FunctionSymbol Visit(FunctionToken functionToken, PouInterfaceSyntax context)
			{
				var allParameters = BindParameters(context.VariableDeclarations).Concat(BindReturnValue(context.Name.ToCaseInsensitive(), context.ReturnDeclaration));
				var uniqueParameters = allParameters.ToOrderedSymbolSetWithDuplicates(Messages);
				return new FunctionSymbol(
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
					: projectBinder.SystemScope.DInt;
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

		public static LazyBoundModule Bind(
			ImmutableArray<ParsedTopLevelInterfaceAndBodyPouLanguageSource> pous,
			ImmutableArray<GlobalVariableLanguageSource> gvls,
			ImmutableArray<ParsedDutLanguageSource> duts)
		{
			if (gvls.Any())
				throw new NotImplementedException();
			var binder = new ProjectBinder(duts);
			return binder.Bind(pous);
		}

		private LazyBoundModule Bind(ImmutableArray<ParsedTopLevelInterfaceAndBodyPouLanguageSource> pous)
		{
			var typeSymbols = WorkingTypeSymbols.ToSymbolSet(symbolInWork => symbolInWork.CompleteSymbolic(this));
			foreach (var typeSymbol in typeSymbols)
				DelayedLayoutType.RecursiveLayout(typeSymbol, MessageBag, typeSymbol.DeclaringPosition);
			foreach (var enumTypeSymbol in typeSymbols.OfType<EnumTypeSymbol>())
				enumTypeSymbol.RecursiveInitializers(MessageBag);

			var functionSymbols = pous.ToSymbolSetWithDuplicates(MessageBag, x => PouSymbolCreator.ConvertToSymbol(x.Interface));

			var itf = new BoundModuleInterface(typeSymbols, functionSymbols);
			return new LazyBoundModule(MessageBag.ToImmutable(), itf);
		}

		public override ErrorsAnd<ITypeSymbol> LookupType(CaseInsensitiveString identifier, SourcePosition sourcePosition) =>
			WorkingTypeSymbols.TryGetValue(identifier, out var symbolInWork)
				? ErrorsAnd.Create(symbolInWork.Symbol)
				: base.LookupType(identifier, sourcePosition);
	}
}
