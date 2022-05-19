using CmdParse;
using Microsoft.Extensions.Logging;
using Runtime.IR;
using StandardLibraryExtensions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace DebugAdapter
{
    using Runtime = Runtime.RTE;
    public static class Program
	{
		private sealed class CmdArgs
		{
			[CmdName("folder")]
			[System.Diagnostics.CodeAnalysis.AllowNull]
			public DirectoryInfo Folder { get; init; }
			[CmdName("entrypoint")]
			[System.Diagnostics.CodeAnalysis.AllowNull]
			public string Entrypoint { get; init; }
			[CmdName("stacksize")]
			[CmdDefault(1024*10)]
			public int StackSize { get; init; }

			[CmdName("runDebugAdapter")]
			[CmdDefault(false)]
			public bool RunDebugAdapter { get; init; }

			[CmdName("launchDebuggerAtStartup")]
			[CmdDefault(false)]
			public bool LaunchDebuggerAtStartup { get; init; }
		}
		static int Main(string[] args)
		{
			if(args.Contains("--launchDebuggerAtStartup"))
				System.Diagnostics.Debugger.Launch();

			return CommandLineParser.Call<CmdArgs>(args, realArgs => RealMain(realArgs));
		}
        static int RealMain(CmdArgs args)
        {
            var pous = LoadPous(args.Folder);
            var gvls = LoadGvls(args.Folder);

            var entrypoint = pous.Values.FirstOrDefault(p => p.Id.Name.Equals(args.Entrypoint, StringComparison.InvariantCultureIgnoreCase));
            if (entrypoint == null)
            {
                Console.Error.WriteLine($"No pou '{args.Entrypoint}' exists.");
                Console.Error.WriteLine($"Avaiable pous are:");
                foreach (var pou in pous.Keys.OrderBy(x => x.Name))
                    Console.Error.WriteLine(pou.Name);
                return 1;
            }
            if (entrypoint.InputArgs.Length != 0)
            {
                Console.Error.WriteLine($"Entry point must have not arguments, but '{args.Entrypoint}' has '{entrypoint.InputArgs.Length}' input(s).");
                return 2;
            }

            var areaSizes = new int[] { 0, args.StackSize }.Concat(EnumerableExtensions.IndexedValuesToEnumerable(gvls.Select(g => KeyValuePair.Create((int)g.Value.Area, (int)g.Value.Size)), 0)).ToImmutableArray();
            var runtime = new Runtime(areaSizes, pous);

            if (args.RunDebugAdapter)
            {
                using var streamIn = Console.OpenStandardInput();
                using var streamOut = Console.OpenStandardOutput();
                using var logger = new SimpleFileLogger("log.txt");
                logger.Log(LogLevel.None, "Starting debug adpater");
                try
                {
                    DebugAdapter.Run(streamIn, streamOut, runtime, logger, pous.Values.ToImmutableArray(), gvls.Values.ToImmutableArray(), entrypoint.Id);
                }
                catch (Exception e)
                {
                    logger.LogCritical(e, "DebugAdapter.Run: ");
                    return 1;
                }
            }
            else
            {
                runtime.RunOnce(entrypoint.Id);
            }

            return 0;
        }

        private static ImmutableDictionary<string, CompiledGlobalVariableList> LoadGvls(DirectoryInfo folder)
        {
            var gvls = ImmutableDictionary.CreateBuilder<string, CompiledGlobalVariableList>();
            foreach (var file in folder.GetFiles("*.gvl.ir.xml"))
            {
                var text = File.ReadAllText(file.FullName);
                var gvl = global::Runtime.IR.Xml.XmlGlobalVariableList.Parse(text);
                gvls.Add(gvl.Name, gvl);
            }

            return gvls.ToImmutable();
        }

        private static ImmutableDictionary<PouId, CompiledPou> LoadPous(DirectoryInfo folder)
        {
            var pous = ImmutableDictionary.CreateBuilder<PouId, CompiledPou>();
            foreach (var file in folder.GetFiles("*.pou.ir.xml"))
            {
                var text = File.ReadAllText(file.FullName);
                var pou = Parser.ParsePou(text);
                pous.Add(pou.Id, pou);
            }

            return pous.ToImmutable();
        }
    }
}
