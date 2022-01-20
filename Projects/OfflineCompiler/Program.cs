using System;
using System.IO;
using CmdParse;

namespace OfflineCompiler
{
	public static class Program
	{
		private sealed class CmdArgs
		{
			[CmdName("folder")]
			[CmdFree(0)]
			public DirectoryInfo Folder { get; init; }
		}

		public static int Main(string[] args) => CommandLineParser.Call<CmdArgs>(args, RealMain);
		private static int RealMain(CmdArgs args)
		{
			try
			{
				OfflineCompiler.Compile(args.Folder, Console.Out);
				return 0;
			}
			catch (Exception e)
			{
				Console.Error.WriteLine($"Internal error: {e.Message}.");
				if (e.StackTrace is string stacktrace)
					Console.Error.WriteLine($"Stacktrace:\n{stacktrace}.");
				return 1;
			}
		}
	}
}
