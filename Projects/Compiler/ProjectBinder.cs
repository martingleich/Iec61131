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

		public BoundModuleInterface(SymbolSet<ITypeSymbol> dutTypes)
		{
			DutTypes = dutTypes;
		}
	}

	public sealed class ProjectBinder : IScope
	{
		private readonly SymbolSet<ITypeSymbolInWork> WorkingTypeSymbols;
		private readonly MessageBag MessageBag = new();

		private ProjectBinder(ImmutableArray<DutLanguageSource> duts)
		{
			WorkingTypeSymbols = duts.ToSymbolSetWithDuplicates(
				MessageBag,
				v => v.Syntax.TypeBody.Accept(SymbolCreator.Instance, v.Syntax));
		}

		private sealed class SymbolCreator : ITypeDeclarationBodySyntax.IVisitor<ITypeSymbolInWork, TypeDeclarationSyntax>
		{
			public static readonly SymbolCreator Instance = new ();
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
					: BuiltInType.DInt;
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

		public EnumTypeSymbol? CurrentEnum => null;

		public static LazyBoundModule Bind(
			ImmutableArray<TopLevelInterfaceAndBodyPouLanguageSource> pous,
			ImmutableArray<GlobalVariableLanguageSource> gvls,
			ImmutableArray<DutLanguageSource> duts)
		{
			var binder = new ProjectBinder(duts);
			return binder.Bind();
		}

		private LazyBoundModule Bind()
		{
			var typeSymbols = WorkingTypeSymbols.ToSymbolSet(symbolInWork => symbolInWork.CompleteSymbolic(this));
			foreach (var typeSymbol in typeSymbols)
				DelayedLayoutType.RecursiveLayout(typeSymbol, MessageBag, default);
			foreach (var enumTypeSymbol in typeSymbols.OfType<EnumTypeSymbol>())
				enumTypeSymbol.RecursiveInitializers(MessageBag, default);

			var itf = new BoundModuleInterface(typeSymbols);
			return new LazyBoundModule(MessageBag.ToImmutable(), itf);
		}


		public ErrorsAnd<ITypeSymbol> LookupType(CaseInsensitiveString identifier, SourcePosition sourcePosition) =>
			WorkingTypeSymbols.TryGetValue(identifier, out var symbolInWork)
				? ErrorsAnd.Create(symbolInWork.Symbol)
				: ErrorsAnd.Create(
					ITypeSymbol.CreateError(sourcePosition, identifier),
					new TypeNotFoundMessage(identifier.Original, sourcePosition));

		public ErrorsAnd<IVariableSymbol> LookupVariable(CaseInsensitiveString identifier, SourcePosition sourcePosition) =>
			ErrorsAnd.Create(
				IVariableSymbol.CreateError(sourcePosition, identifier),
				new VariableNotFoundMessage(identifier.Original, sourcePosition));
	}
}
