namespace Rainbow.SourceControl
{
	public interface ISourceControlManager
	{
		bool Edit(string filename);
		bool Add(string filename);
		bool Remove(string filename);
	}
}