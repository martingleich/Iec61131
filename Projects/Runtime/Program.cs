using CmdParse;
using Runtime.IR;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Runtime
{
	class Program
	{
		private sealed class CmdArgs
		{
			[CmdName("folder")]
			public DirectoryInfo Folder { get; init; }
			[CmdName("entrypoint")]
			public string Entrypoint { get; init; }
			[CmdName("stacksize")]
			[CmdDefault(1024*10)]
			public int StackSize { get; init; }
		}
		static void Main(string[] args) => CommandLineParser.Call<CmdArgs>(args, RealMain);
		static int RealMain(CmdArgs args)
		{
			var pous = ImmutableDictionary.CreateBuilder<PouId, CompiledPou>();
			foreach (var file in args.Folder.GetFiles("*.ir"))
			{
				var text = File.ReadAllText(file.FullName);
				var pou = IR.Parser.ParsePou(text);
				pous.Add(pou.Id, pou);
			}
			PouId? entrypoint = pous.Keys.FirstOrDefault(p => p.Callee.Equals(args.Entrypoint, StringComparison.InvariantCultureIgnoreCase));
			if (!entrypoint.HasValue)
			{
				Console.Error.WriteLine($"No pou '{args.Entrypoint}' exists.");
				Console.Error.WriteLine($"Avaiable pous are:");
				foreach(var pou in pous.Keys.OrderBy(x => x.Callee))
					Console.Error.WriteLine(pou.Callee);
				return 1;
			}
			var called = pous[entrypoint.Value];
			if (called.InputArgs.Length != 0)
			{
				Console.Error.WriteLine($"Entry point must have not arguments, but '{args.Entrypoint}' has '{called.InputArgs.Length}' input(s).");
				return 2;
			}

			var runtime = new IR.Runtime(new[] { 0, args.StackSize }, pous.ToImmutable());
			runtime.Init(entrypoint.Value);
			while (runtime.Step())
				;
			return 0;
		}
	}
}
