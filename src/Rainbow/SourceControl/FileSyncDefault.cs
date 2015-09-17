namespace Rainbow.SourceControl
{
	public class FileSyncDefault : ISourceControlSync
	{
		public bool DeletePreProcessing(string filename)
		{
			return true;
		}

		public bool EditPreProcessing(string filename)
		{
			return true;
		}

		public bool EditPostProcessing(string filename)
		{
			return true;
		}
	}
}