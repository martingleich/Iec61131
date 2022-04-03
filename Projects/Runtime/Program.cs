using CmdParse;
using Runtime.IR;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Runtime
{
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
		}
		static int Main(string[] args)
		{
			return CommandLineParser.Call<CmdArgs>(args, realArgs => RealMain(realArgs).GetAwaiter().GetResult());
		}
		static async Task<int> RealMain(CmdArgs args)
		{
			var pous = ImmutableDictionary.CreateBuilder<PouId, CompiledPou>();
			foreach (var file in args.Folder.GetFiles("*.ir"))
			{
				var text = File.ReadAllText(file.FullName);
				var pou = Parser.ParsePou(text);
				pous.Add(pou.Id, pou);
			}
			PouId entrypoint = pous.Keys.FirstOrDefault(p => p.Name.Equals(args.Entrypoint, StringComparison.InvariantCultureIgnoreCase));
			if (entrypoint.Name == null)
			{
				Console.Error.WriteLine($"No pou '{args.Entrypoint}' exists.");
				Console.Error.WriteLine($"Avaiable pous are:");
				foreach(var pou in pous.Keys.OrderBy(x => x.Name))
					Console.Error.WriteLine(pou.Name);
				return 1;
			}
			var called = pous[entrypoint];
			if (called.InputArgs.Length != 0)
			{
				Console.Error.WriteLine($"Entry point must have not arguments, but '{args.Entrypoint}' has '{called.InputArgs.Length}' input(s).");
				return 2;
			}

			var runtime = new Runtime(new[] { 0, args.StackSize }, pous.ToImmutable(), entrypoint);

			if (args.RunDebugAdapter)
			{
				System.Diagnostics.Debugger.Launch();
				using var streamIn = Console.OpenStandardInput();
				using var streamOut = Console.OpenStandardOutput();
				using var logFile = File.OpenWrite("log.txt");
				using var logStream = new StreamWriter(logFile, System.Text.Encoding.UTF8);
				logStream.WriteLine($"Starting debug adpater at {DateTime.Now}.");
				DebugAdapter.Run(streamIn, streamOut, runtime, logStream);
			}
			else
			{
				runtime.RunOnce();
			}

			return 0;
		}
	}
}
