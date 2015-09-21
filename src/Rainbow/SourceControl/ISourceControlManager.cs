namespace Rainbow.SourceControl
{
	public interface ISourceControlManager
	{
		ISourceControlSync SourceControlSync { get; }
		bool AllowFileSystemClear { get; }
		bool EditPreProcessing(string filename);
		bool EditPostProcessing(string filename);
		bool DeletePreProcessing(string filename);
	}
}