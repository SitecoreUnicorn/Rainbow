namespace Rainbow.SourceControl
{
	public class FileSyncDefault : ISourceControlSync
	{
		public bool AllowFileSystemClear { get { return true; } }

		public bool DeletePreProcessing(string filename)
		{
			return true;
		}

		public bool DeleteRecursivePreProcessing(string path)
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