namespace Runtime.IR
{
	public interface IStatement
	{
		int? Execute(Runtime runtime);
	}
}
