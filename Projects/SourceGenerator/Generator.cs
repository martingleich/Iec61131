using Microsoft.CodeAnalysis;
using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Collections.Generic;
using StandardLibraryExtensions;

namespace SourceGenerator
{
	[Generator]
	public class Generator : ISourceGenerator
	{
		[Conditional("DEBUG")]
		private void AttachDebuggerIfDebug()
		{
            if (!Debugger.IsAttached)
                Debugger.Launch();
		}

		public void Execute(GeneratorExecutionContext context)
		{
			AttachDebuggerIfDebug();
			var config = LoadConfig(context);
			try
			{
				ExecuteUnsafe(config, context);
			}
			catch(Exception e)
			{
				context.ReportDiagnostic(Diagnostic.Create(
					InternalError,
					Location.Create(config.ObjectsFilePath,
						new Microsoft.CodeAnalysis.Text.TextSpan(0, 1),
						new Microsoft.CodeAnalysis.Text.LinePositionSpan(
							new Microsoft.CodeAnalysis.Text.LinePosition(0, 0),
							new Microsoft.CodeAnalysis.Text.LinePosition(0, 1))),
					FlattenString(e.ToString() + "@" + e.StackTrace)));
			}
		}

		private string FlattenString(string input)
		{
			return input.Split(new char[] { '\r', '\n' }).Select(str => str.Trim()).Where(str => str.Length > 0).DelimitWith("||");
		}
		public void ExecuteUnsafe(Configuration config, GeneratorExecutionContext context)
		{
			List<string> errors = new List<string>();
			var objects = LoadObjects(config.ObjectsFilePath, context, errors);

			if (config.GenerateTestCode)
			{
				context.AddSource("TokenTestHelper.g.cs", objects.GetScannerTestHelperString());
				context.AddSource("SyntaxTestHelper.g.cs", objects.GetSyntaxTestHelperString());
			}
			else
			{
				context.AddSource("TokenInterfaces.g.cs", objects.GetTokenInterfacesString());
				context.AddSource("Tokens.g.cs", objects.GetTokensString());
				context.AddSource("SyntaxInterfaces.g.cs", objects.GetSyntaxInterfacesString());
				context.AddSource("SyntaxNodes.g.cs", objects.GetSyntaxNodesString());
			}
		}
		public void Initialize(GeneratorInitializationContext context)
		{
			// Nothing to do here.
		}

		private static T DeserializeFromFile<T>(string path)
		{
			var xmlSerializer = new XmlSerializer(typeof(T));
			xmlSerializer.UnknownElement += (object sender, XmlElementEventArgs e) =>
			{
				throw new InvalidOperationException($"Unknown xml element {e.Element.Name}.");
			};
			using var file = File.OpenRead(path);
			return (T)xmlSerializer.Deserialize(file);
		}

		private static Configuration LoadConfig(GeneratorExecutionContext context)
		{
			var configPath = context.AdditionalFiles.FirstOrDefault(f => Path.GetFileName(f.Path) == "SourceGenerator.settings.xml")?.Path;
			Configuration config;
			if (configPath == null)
				config = new Configuration() { GenerateTestCode = false };
			else
				config = DeserializeFromFile<Configuration>(configPath);
			var objectsFilePath = context.AdditionalFiles.FirstOrDefault(f => Path.GetFileName(f.Path) == "SourceGenerator.xml")?.Path;
			config.ObjectsFilePath = objectsFilePath;
			return config;
		}
		private static readonly DiagnosticDescriptor MissingElementError = new("MissingElement", "Missing element", "Missing element: {0}", "SourceGenerator", DiagnosticSeverity.Error, true);
		private static readonly DiagnosticDescriptor InternalError = new("InternalError", "Internal error", "Internal error: {0}", "SourceGenerator", DiagnosticSeverity.Error, true);
		private static Objects LoadObjects(string objectsFilePath, GeneratorExecutionContext context, List<string> errors)
		{
			var objects = DeserializeFromFile<Objects>(objectsFilePath);
			objects.Initialize(errors);
			var fileContent = File.ReadAllLines(objectsFilePath);
			foreach(var error in errors.Distinct())
			{
				int fullIndex = 0;
				int lineOffset = 0;
				int lineId = 0;
				for (; lineId < fileContent.Length; ++lineId)
				{
					lineOffset = fileContent[lineId].IndexOf(error);
					if (lineOffset > 0)
					{
						fullIndex += lineOffset;
						break;
					}
					fullIndex += fileContent[lineId].Length;
				}
				if (lineId == fileContent.Length)
				{
					fullIndex = 0;
					lineId = 0;
					lineOffset = 0;
				}
				context.ReportDiagnostic(Diagnostic.Create(
					MissingElementError,
					Location.Create(objectsFilePath,
						new Microsoft.CodeAnalysis.Text.TextSpan(fullIndex, error.Length),
						new Microsoft.CodeAnalysis.Text.LinePositionSpan(
							new Microsoft.CodeAnalysis.Text.LinePosition(lineId, lineOffset),
							new Microsoft.CodeAnalysis.Text.LinePosition(lineId, lineOffset + error.Length))),
					error));
			}
			return objects;
		}
	}
}
