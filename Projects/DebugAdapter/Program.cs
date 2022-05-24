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

			[CmdName("logPath")]
			[CmdDefault(null)]
			public FileInfo? LogPath { get; init; }
		}
		static int Main(string[] args)
		{
			if(args.Contains("--launchDebuggerAtStartup"))
				System.Diagnostics.Debugger.Launch();

			return CommandLineParser.Call<CmdArgs>(args, realArgs => RealMain(realArgs));
		}
        static int RealMain(CmdArgs args)
        {
            var module = CompiledModule.LoadFromDirectory(args.Folder);

            var entrypoint = module.Pous.FirstOrDefault(p => p.Id.Name.Equals(args.Entrypoint, StringComparison.InvariantCultureIgnoreCase));
            if (entrypoint == null)
            {
                Console.Error.WriteLine($"No pou '{args.Entrypoint}' exists.");
                Console.Error.WriteLine($"Avaiable pous are:");
                foreach (var pou in module.Pous.OrderBy(x => x.Id.Name))
                    Console.Error.WriteLine(pou.Id.Name);
                return 1;
            }
            if (entrypoint.InputArgs.Length != 0)
            {
                Console.Error.WriteLine($"Entry point must have not arguments, but '{args.Entrypoint}' has '{entrypoint.InputArgs.Length}' input(s).");
                return 2;
            }

            var areaSizes = 
                Enumerable.Concat(
                    new [] {
                        KeyValuePair.Create(0, 0),
                        KeyValuePair.Create(1, args.StackSize)
                    },
                    module.GlobalVariableLists.Select(g => KeyValuePair.Create((int)g.Area, (int)g.Size)))
                .IndexedValuesToEnumerable(0)
                .ToImmutableArray();
            var runtime = new Runtime(areaSizes, module.Pous.ToImmutableDictionary(x => x.Id));

            if (args.RunDebugAdapter)
            {
                using var streamIn = Console.OpenStandardInput();
                using var streamOut = Console.OpenStandardOutput();
                ILogger logger = args.LogPath is FileInfo logPath ? new SimpleFileLogger(logPath) : new NullLogger();
                logger.Log(LogLevel.None, "Starting debug adpater");
                try
                {
                    DebugAdapter.Run(streamIn, streamOut, runtime, logger, module, entrypoint.Id);
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

    }
}
