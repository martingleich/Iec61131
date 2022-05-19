using System.IO;

namespace Runtime.IR
{
    public interface ICompiledObject
	{
		public string ResultFileName { get; }
		public void WriteToStream(Stream target);
	}
}
