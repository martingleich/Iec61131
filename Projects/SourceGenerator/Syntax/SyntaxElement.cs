using System.Collections.Generic;
using System.Xml.Serialization;

namespace SourceGenerator
{
	public class SyntaxElement
	{
		[XmlElement("Name")]
		public string Name { get; set; }
		[XmlElement("Kind")]
		public ElementKind Kind { get; set; }
		[XmlElement("Type")]
		public string BaseTypeName { get; set; }

		public void Initialize(Objects objects, List<string> errors)
		{
			BaseType = objects.GetBasicSyntaxElementTypeByName(BaseTypeName);
			if (BaseType == null)
				errors.Add(BaseTypeName);
		}

		public string BaseName => IsToken ? Name.Replace("Token", "") : Name;
		[XmlIgnore]
		public IBasicSyntaxElementType BaseType { get; private set; }

		public string TypeName => Kind switch
		{
			ElementKind.Normal => BaseTypeName,
			ElementKind.Nullable => $"{BaseTypeName}?",
			ElementKind.Array => $"SyntaxArray<{BaseTypeName}>",
			ElementKind.CommaSeparated => $"SyntaxCommaSeparated<{BaseTypeName}>",
			_ => throw new System.NotImplementedException()
		};
		public string ArgName => LanguageUtils.GetValidInstanceIdentifier(Name);
		public bool IsToken => BaseType is TokenDescription || BaseType is TokenInterface;
		public bool IsTokenWithValue => BaseType is TokenDescriptionWithValue;
		public bool IsTokenWithoutValue => BaseType is TokenDescriptionWithoutValue | BaseType is TokenDescriptionKeyword;
		public bool IsTokenInterface => BaseType is TokenInterface;
		public bool IsNullable => Kind == ElementKind.Nullable;

		public override string ToString() => $"Name : TypeName";
	}
}