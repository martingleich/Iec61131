using System;
using System.IO;
using Compiler;
using Compiler.Messages;
using System.Linq;
using StandardLibraryExtensions;
using System.Collections.Generic;

namespace OfflineCompiler
{
	public static class OfflineCompiler
	{
		public static void Compile(
			DirectoryInfo folder,
			DirectoryInfo build,
			TextWriter stdout)
		{
			var sourceMap = new SourceMap();
			var sources = new List<ILanguageSource>();
			foreach (var (lmSource, sourcemap) in folder.EnumerateFiles().Select(ToLanguageSource).WhereNotNullStruct())
			{
				sources.Add(lmSource);
				sourceMap.Add(sourcemap);
			}

			bool isOkay = true;
			var project = Project.New(folder.Name.ToCaseInsensitive(), sources);
			foreach (var msg in Enumerable.Concat(project.ParseMessages, project.BoundModule.BindMessages))
			{
				stdout.WriteLine(GetMessageText(msg, sourceMap));
				isOkay &= !msg.Critical;
			}

			if (build != null && isOkay)
			{
				build.Create();
				foreach (var pou in project.BoundModule.FunctionPous)
				{
					var codegen = new CodegenIR(pou.Value);
					codegen.CompileInitials(pou.Value.LocalVariables);
					codegen.CompileStatement(pou.Value.BoundBody.Value);
					var code = codegen.GetGeneratedCode();
					var resultFile = build.FileInfo($"{pou.Key.Name}.ir");
					File.WriteAllText(resultFile.FullName, code.ToString(), System.Text.Encoding.UTF8);
				}
			}
		}
		static string GetMessageText(IMessage msg, SourceMap sourceMap)
		{
			var kind = msg.Critical ? "Error" : "Warning";
			return $"{kind}@{sourceMap.GetNameOf(msg.Span)}: {msg.Text}";
		}

		private static TopLevelPouLanguageSource ToLanguageSourcePou(FileInfo info, string content)
			=> new(info.Name, content);
		private static GlobalVariableListLanguageSource ToLanguageSourceGvl(FileInfo info, string content)
			=> new(info.Name, info.Name.ToCaseInsensitive(), content);
		private static DutLanguageSource ToLanguageSourceDut(FileInfo info, string content)
			=> new(info.Name, content);

		private static string Extension(string name, out string remainder)
		{
			int id = name.LastIndexOf(".");
			if (id < 0)
			{
				remainder = name;
				return "";
			}
			else
			{
				remainder = name.Remove(id);
				return name[id..];
			}
		}
		private static (ILanguageSource, SourceMap.SingleFile)? ToLanguageSource(FileInfo info)
		{
			if (Extension(info.Name, out string remainder).Equals(".ST", StringComparison.InvariantCultureIgnoreCase))
			{
				return Extension(remainder, out var _).ToUpperInvariant() switch
				{
					".POU" => ToLanguageSource2(info, ToLanguageSourcePou),
					".GVL" => ToLanguageSource2(info, ToLanguageSourceGvl),
					".DUT" => ToLanguageSource2(info, ToLanguageSourceDut),
					_ => null,
				};
			}
			else
			{
				return null;
			}
		}
		private static (ILanguageSource, SourceMap.SingleFile)? ToLanguageSource2(FileInfo info, Func<FileInfo, string, ILanguageSource> creator)
		{
			var content = File.ReadAllText(info.FullName, System.Text.Encoding.UTF8);
			var sourceMap = SourceMap.SingleFile.Create(info.Name, content);
			return (creator(info, content), sourceMap);
		}
	}
}
