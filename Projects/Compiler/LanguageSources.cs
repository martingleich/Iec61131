using Compiler.Messages;
using System;
using System.Collections.Immutable;

namespace Compiler
{
	public sealed class TopLevelInterfaceAndBodyPouLanguageSource : ILanguageSource
	{
		public readonly string Interface;
		public readonly string Body;

		public TopLevelInterfaceAndBodyPouLanguageSource(string @interface, string body)
		{
			Interface = @interface ?? throw new ArgumentNullException(nameof(@interface));
			Body = body ?? throw new ArgumentNullException(nameof(body));
		}

		void ILanguageSource.Accept(ILanguageSource.IVisitor visitor) => visitor.Visit(this);
	}

	public sealed class GlobalVariableLanguageSource : ILanguageSource
	{
		public readonly string Name;
		public readonly GlobalVarListSyntax Syntax;

		public GlobalVariableLanguageSource(string name, GlobalVarListSyntax syntax)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Syntax = syntax ?? throw new ArgumentNullException(nameof(syntax));
		}

		void ILanguageSource.Accept(ILanguageSource.IVisitor visitor) => visitor.Visit(this);
	}

	public sealed class DutLanguageSource : ILanguageSource
	{
		public readonly string Source;

		public DutLanguageSource(string source)
		{
			Source = source ?? throw new ArgumentNullException(nameof(source));
		}

		void ILanguageSource.Accept(ILanguageSource.IVisitor visitor) => visitor.Visit(this);
	}

	public struct ParsedDutLanguageSource
	{
		public readonly DutLanguageSource Original;
		public readonly TypeDeclarationSyntax Syntax;
		public readonly ImmutableArray<IMessage> Messages;

		public ParsedDutLanguageSource(DutLanguageSource original, TypeDeclarationSyntax syntax, ImmutableArray<IMessage> messages)
		{
			Original = original ?? throw new ArgumentNullException(nameof(original));
			Syntax = syntax ?? throw new ArgumentNullException(nameof(syntax));
			Messages = messages;
		}
	}

	public struct ParsedTopLevelInterfaceAndBodyPouLanguageSource
	{
		public readonly TopLevelInterfaceAndBodyPouLanguageSource Original;
		public readonly PouInterfaceSyntax Interface;
		public readonly StatementListSyntax Body;
		public readonly ImmutableArray<IMessage> Messages;

		public ParsedTopLevelInterfaceAndBodyPouLanguageSource(TopLevelInterfaceAndBodyPouLanguageSource original, PouInterfaceSyntax @interface, StatementListSyntax body, ImmutableArray<IMessage> messages)
		{
			Original = original ?? throw new ArgumentNullException(nameof(original));
			Interface = @interface ?? throw new ArgumentNullException(nameof(@interface));
			Body = body ?? throw new ArgumentNullException(nameof(body));
			Messages = messages;
		}
	}
}
