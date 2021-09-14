using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace SourceGenerator
{
	public abstract class TokenDescription : IBasicSyntaxElementType
	{
		[XmlElement("Name")]
		public string Name { get; set; }
		[XmlElement("Interface")]
		public List<string> Interfaces { get; set; }

		public string ArgName => LanguageUtils.GetValidInstanceIdentifier(Name);

		public abstract Code ToCode();
		public abstract Code ToTestCode();

		public List<string> AllInterfaces { get; set; }
		public void Initialize(Objects objects, List<string> errors)
		{
			var allInterfaces = new HashSet<string>();
			CollectAllBaseInterfaces(objects, errors, Interfaces, allInterfaces);
			AllInterfaces = allInterfaces.OrderBy(x => x).ToList();
		}
		private void CollectAllBaseInterfaces(Objects objects, List<string> errors, List<string> toCollect, HashSet<string> collected)
		{
			foreach (var x in toCollect)
			{
				if (collected.Add(x))
				{
					var y = objects.GetTokenInterfaceByName(x);
					if (y == null)
						errors.Add(x);
					CollectAllBaseInterfaces(objects, errors, y.BaseInterfaces, collected);
				}
			}

		}

		public Code GetInterfaceImplCode()
		{
			var cw = new CodeWriter();
			foreach (var itf in AllInterfaces)
			{
				cw.WriteLine($"T {itf}.Accept<T>({itf}.IVisitor<T> visitor) => visitor.Visit(this);");
				cw.WriteLine($"T {itf}.Accept<T, TContext>({itf}.IVisitor<T, TContext> visitor, TContext context) => visitor.Visit(this, context);");
			}
			return cw.ToCode();
		}
	}
}
