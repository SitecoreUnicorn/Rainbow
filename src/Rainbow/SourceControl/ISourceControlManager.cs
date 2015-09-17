namespace Rainbow.SourceControl
{
	public interface ISourceControlManager
	{
		bool EditPreProcessing(string filename);
		bool EditPostProcessing(string filename);
		bool DeletePreProcessing(string filename);
	}
}