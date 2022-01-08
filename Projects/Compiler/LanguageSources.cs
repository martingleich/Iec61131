using Compiler.Messages;
using System;
using System.Collections.Immutable;

namespace Compiler
{
	public sealed class TopLevelInterfaceAndBodyPouLanguageSource : ILanguageSource
	{
		public string File { get; }
		public readonly string Interface;
		public readonly string Body;

		public TopLevelInterfaceAndBodyPouLanguageSource(string file, string @interface, string body)
		{
			File = file ?? throw new ArgumentNullException(nameof(file));
			Interface = @interface ?? throw new ArgumentNullException(nameof(@interface));
			Body = body ?? throw new ArgumentNullException(nameof(body));
		}

		void ILanguageSource.Accept(ILanguageSource.IVisitor visitor) => visitor.Visit(this);
	}

	public sealed class TopLevelPouLanguageSource : ILanguageSource
	{
		public string File { get; }
		public readonly string Code;

		public TopLevelPouLanguageSource(string file, string code)
		{
			File = file ?? throw new ArgumentNullException(nameof(file));
			Code = code;
		}

		void ILanguageSource.Accept(ILanguageSource.IVisitor visitor) => visitor.Visit(this);
	}

	public sealed class GlobalVariableListLanguageSource : ILanguageSource
	{
		public string File { get; }
		public readonly CaseInsensitiveString Name;
		public readonly string Body;

		public GlobalVariableListLanguageSource(string file, CaseInsensitiveString name, string body)
		{
			File = file ?? throw new ArgumentNullException(nameof(file));
			Name = name;
			Body = body ?? throw new ArgumentNullException(nameof(body));
		}

		void ILanguageSource.Accept(ILanguageSource.IVisitor visitor) => visitor.Visit(this);
	}

	public sealed class DutLanguageSource : ILanguageSource
	{
		public string File { get; }
		public readonly string Source;

		public DutLanguageSource(string file, string source)
		{
			File = file ?? throw new ArgumentNullException(nameof(file));
			Source = source ?? throw new ArgumentNullException(nameof(source));
		}

		void ILanguageSource.Accept(ILanguageSource.IVisitor visitor) => visitor.Visit(this);
	}

	public readonly struct ParsedDutLanguageSource
	{
		public readonly DutLanguageSource Original;
		public readonly TypeDeclarationSyntax Syntax;
		public readonly ImmutableArray<IMessage> Messages;

		public static ParsedDutLanguageSource FromSource(DutLanguageSource source)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));
			var msg = new MessageBag();
			var parsed = Parser.ParseTypeDeclaration(source.File, source.Source, msg);
			return new (source, parsed, msg.ToImmutable());
		}
		public ParsedDutLanguageSource(DutLanguageSource original, TypeDeclarationSyntax syntax, ImmutableArray<IMessage> messages)
		{
			Original = original ?? throw new ArgumentNullException(nameof(original));
			Syntax = syntax ?? throw new ArgumentNullException(nameof(syntax));
			Messages = messages;
		}
	}

	public readonly struct ParsedTopLevelInterfaceAndBodyPouLanguageSource
	{
		public readonly ILanguageSource Original;
		public readonly PouInterfaceSyntax Interface;
		public readonly StatementListSyntax Body;
		public readonly ImmutableArray<IMessage> Messages;

		public static ParsedTopLevelInterfaceAndBodyPouLanguageSource FromSource(TopLevelPouLanguageSource source)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));
			var msg = new MessageBag();
			var (itf, body) = Parser.ParsePou(source.File, source.Code, msg);
			return new (source, itf, body, msg.ToImmutable());
		}
		public static ParsedTopLevelInterfaceAndBodyPouLanguageSource FromSource(TopLevelInterfaceAndBodyPouLanguageSource source)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));
			var msg = new MessageBag();
			var itf = Parser.ParsePouInterface(source.File + "/itf", source.Interface, msg);
			var body = Parser.ParsePouBody(source.File + "/impl", source.Body, msg);
			return new (source, itf, body, msg.ToImmutable());
		}

		public ParsedTopLevelInterfaceAndBodyPouLanguageSource(ILanguageSource original, PouInterfaceSyntax @interface, StatementListSyntax body, ImmutableArray<IMessage> messages)
		{
			Original = original ?? throw new ArgumentNullException(nameof(original));
			Interface = @interface ?? throw new ArgumentNullException(nameof(@interface));
			Body = body ?? throw new ArgumentNullException(nameof(body));
			Messages = messages;
		}
	}

	public readonly struct ParsedGVLLanguageSource
	{
		public readonly GlobalVariableListLanguageSource Original;
		public readonly CaseInsensitiveString Name;
		public readonly GlobalVarListSyntax Syntax;
		public readonly ImmutableArray<IMessage> Messages;

		public static ParsedGVLLanguageSource FromSource(GlobalVariableListLanguageSource source)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));
			var msg = new MessageBag();
			var body = Parser.ParseGlobalVarList(source.File, source.Body, msg);
			return new (source, source.Name, body, msg.ToImmutable());
		}
		public ParsedGVLLanguageSource(GlobalVariableListLanguageSource original, CaseInsensitiveString name, GlobalVarListSyntax syntax, ImmutableArray<IMessage> messages)
		{
			Original = original ?? throw new ArgumentNullException(nameof(original));
			Name = name;
			Syntax = syntax ?? throw new ArgumentNullException(nameof(syntax));
			Messages = messages;
		}
	}

	public readonly struct LibraryLanguageSource
	{
		public readonly SourceSpan SourceSpan;
		public readonly BoundModuleInterface Interface;

		public LibraryLanguageSource(SourceSpan sourceSpan, BoundModuleInterface @interface)
		{
			SourceSpan = sourceSpan;
			Interface = @interface;
		}
		public CaseInsensitiveString Namespace => Interface.Name;
	}
}
