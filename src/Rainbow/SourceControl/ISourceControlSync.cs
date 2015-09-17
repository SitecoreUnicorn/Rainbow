namespace Rainbow.SourceControl
{
	public interface ISourceControlSync
	{
		bool DeletePreProcessing(string filename);
		bool EditPreProcessing(string filename);
		bool EditPostProcessing(string filename);
	}
}