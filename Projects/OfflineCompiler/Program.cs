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
			[System.Diagnostics.CodeAnalysis.AllowNull]
			public DirectoryInfo Folder { get; init; }

			[CmdName("output")]
			[CmdDefault(null)]
			[System.Diagnostics.CodeAnalysis.AllowNull]
			public DirectoryInfo Output { get; init; }
		}

		public static int Main(string[] args) => CommandLineParser.Call<CmdArgs>(args, RealMain);
		private static int RealMain(CmdArgs args)
		{
			try
			{
				OfflineCompiler.Compile(args.Folder, args.Output, Console.Out);
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
