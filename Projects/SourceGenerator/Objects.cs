using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Serialization;

namespace SourceGenerator
{
	[XmlRoot(ElementName="Objects")]
	public class Objects
	{
		[XmlElement("TokenInterfaces")]
		public TokenInterfaces TokenInterfaces { get; set; }
		[XmlElement("TokenClasses")]
		public TokenClasses TokenClasses { get; set; }
		[XmlElement("SyntaxInterfaces")]
		public SyntaxInterfaces SyntaxInterfaces { get; set; }
		[XmlElement("SyntaxClasses")]
		public SyntaxClasses SyntaxClasses { get; set; }

		public IBasicSyntaxElementType GetBasicSyntaxElementTypeByName(string name) => TypeMap.TryGetValue(name, out var value) ? value : null;
		public SyntaxInterface GetSyntaxInterfaceByName(string name) => SyntaxInterfaceMap.TryGetValue(name, out var value) ? value : null;
		public TokenInterface GetTokenInterfaceByName(string name) => TokenInterfaceMap.TryGetValue(name, out var value) ? value : null;
		private Dictionary<string, IBasicSyntaxElementType> TypeMap;
		private Dictionary<string, SyntaxInterface> SyntaxInterfaceMap;
		private Dictionary<string, TokenInterface> TokenInterfaceMap;

		private Dictionary<string, TValue> ToDictionarySafe<TValue>(IEnumerable<TValue> values, Func<TValue, string> keyFunc, List<string> errors)
		{
			Dictionary<string, TValue> result = new Dictionary<string, TValue>();
			foreach (var value in values)
			{
				var key = keyFunc(value);
				if (result.ContainsKey(key))
					errors.Add($"The key '{key}' already exists.");
				else
					result.Add(key, value);
			}
			return result;
		}
		public void Initialize(List<string> errors)
		{
			TypeMap = ToDictionarySafe(
				TokenInterfaces.All.Concat((IEnumerable<IBasicSyntaxElementType>)TokenClasses.All).Concat(SyntaxInterfaces.All).Concat(SyntaxClasses.All),
				x => x.Name, errors);
			SyntaxInterfaceMap = ToDictionarySafe(
				SyntaxInterfaces.All,
				x => x.Name, errors);
			TokenInterfaceMap = ToDictionarySafe(
				TokenInterfaces.All,
				x => x.Name, errors);
			foreach (var desc in TokenClasses.All)
				desc.Initialize(this, errors);
			foreach (var cls in SyntaxClasses.All)
				cls.Initialize(this, errors);
			foreach (var itf in SyntaxInterfaces.All)
				itf.Initialize(this, errors);
		}

		public string GetTokenInterfacesString()
		{
			var cw = new CodeWriter();
			cw.WriteLine("#nullable enable");
			cw.WriteLine("using System.Diagnostics.CodeAnalysis;");
			cw.WriteLine("namespace Compiler");
			cw.StartBlock();
			foreach (var itf in TokenInterfaces.All)
			{
				var impls = TokenClasses.All.Where(tok => tok.AllInterfaces.Contains(itf.Name)).ToImmutableArray();
				cw.WriteCode(itf.ToCode(impls));
			}
			cw.EndBlock();

			return cw.ToString();
		}
		public string GetTokensString()
		{
			var cw = new CodeWriter();
			cw.WriteLine("#nullable enable");
			cw.WriteLine("using System.Diagnostics.CodeAnalysis;");
			cw.WriteLine("using System;");
			cw.WriteLine("namespace Compiler");
			cw.StartBlock();
			cw.WriteLine(@"public abstract class DefaultTokenImplementation : IToken
	{
		protected DefaultTokenImplementation(SourcePoint startPosition, IToken? leadingNonSyntax)
		{
			StartPosition = startPosition;
			LeadingNonSyntax = leadingNonSyntax;
		}

		public SourcePoint StartPosition { get; }
		public abstract string? Generating { get; }
		public IToken? LeadingNonSyntax { get; }
		public IToken? TrailingNonSyntax { get; set; }
		public int Length => Generating != null ? Generating.Length : 0;
		public SourceSpan SourceSpan => SourceSpan.FromStartLength(StartPosition, Length);
	}

	public abstract class DefaultTokenWithValueImplementation<T> : DefaultTokenImplementation, ITokenWithValue<T>
	{
		protected DefaultTokenWithValueImplementation(T value, string? generating, SourcePoint startPosition, IToken? leadingNonSyntax) :
			base(startPosition, leadingNonSyntax)
		{
			Value = value;
			Generating = generating;
		}

		public T Value { get; }
		public override string? Generating { get; }
	}");
			foreach (var tokenClass in TokenClasses.All)
			{
				cw.WriteCode(tokenClass.ToCode());
			}
			cw.WriteCode(TokenDescriptionKeyword.WriteKeywordTable(TokenClasses.All.OfType<TokenDescriptionKeyword>()));
			cw.EndBlock();
			return cw.ToString();
		}
		public string GetSyntaxInterfacesString()
		{
			var cw = new CodeWriter();
			cw.WriteLine("#nullable enable");
			cw.WriteLine("using System.Diagnostics.CodeAnalysis;");
			cw.WriteLine("namespace Compiler");
			cw.StartBlock();
			foreach (var itf in SyntaxInterfaces.All)
			{
				var impls = SyntaxClasses.All.Where(cls => cls.Implements(itf)).ToImmutableArray();
				cw.WriteCode(itf.ToCode(impls));
			}
			cw.EndBlock();

			return cw.ToString();
		}
		public string GetSyntaxNodesString()
		{
			var cw = new CodeWriter();
			cw.WriteLine("#nullable enable");
			cw.WriteLine("using System.Diagnostics.CodeAnalysis;");
			cw.WriteLine("namespace Compiler");
			cw.StartBlock();
			foreach (var cls in SyntaxClasses.All)
				cw.WriteCode(cls.ToCode());
			cw.EndBlock();

			return cw.ToString();
		}
	
		public string GetScannerTestHelperString()
		{
			var cw = new CodeWriter();
			cw.WriteLine("#nullable enable");
			cw.WriteLine("using Compiler;");
			cw.WriteLine("using Xunit;");
			cw.WriteLine("using TokenTest = System.Action<Compiler.IToken?>;");
			cw.WriteLine("namespace CompilerTests");
			cw.StartBlock();
			cw.WriteLine("public static partial class ScannerTestHelper");
			cw.StartBlock();
			foreach (var desc in TokenClasses.All)
			{
				var code = desc.ToTestCode();
				cw.WriteCode(code);

			}
			cw.EndBlock();
			cw.EndBlock();

			return cw.ToString();
		}
		public string GetSyntaxTestHelperString()
		{
			var cw = new CodeWriter();
			cw.WriteLine("#nullable enable");
			cw.WriteLine("using Compiler;");
			cw.WriteLine("using Xunit;");
			cw.WriteLine("using TokenTest = System.Action<Compiler.IToken?>;");
			cw.WriteLine("using SyntaxTest = System.Action<Compiler.ISyntax?>;");
			cw.WriteLine("namespace CompilerTests");
			cw.StartBlock();
			cw.WriteLine("using static ScannerTestHelper;");
			cw.WriteLine("public static partial class ParserTestHelper");
			cw.StartBlock();
			foreach (var desc in SyntaxClasses.All)
			{
				var code = desc.ToTestCode();
				cw.WriteCode(code);

			}
			cw.EndBlock();
			cw.EndBlock();

			return cw.ToString();
		}
	}
}
