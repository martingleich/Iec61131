namespace Runtime.IR
{
	public interface IExpression
	{
		void LoadTo(Runtime runtime, MemoryLocation location, int size);
	}
}
