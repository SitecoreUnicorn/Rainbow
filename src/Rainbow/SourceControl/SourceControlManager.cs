using System;

namespace Rainbow.SourceControl
{
	public class SourceControlManager : ISourceControlManager
	{
		public ISourceControlSync SourceControlSync { get; private set; }
		public bool AllowFileSystemClear { get { return SourceControlSync.AllowFileSystemClear; } }

		public SourceControlManager(ISourceControlSync sourceControlSync)
		{
			SourceControlSync = sourceControlSync;
		}

		public bool EditPreProcessing(string filename)
		{
			bool success = SourceControlSync.EditPreProcessing(filename);
			if (success) return true;

			throw new Exception("[Rainbow] Edit pre-processing failed for " + filename);
		}

		public bool EditPostProcessing(string filename)
		{
			bool success = SourceControlSync.EditPostProcessing(filename);
			if (success) return true;

			throw new Exception("[Rainbow] Edit post-processing failed for " + filename);
		}

		public bool DeletePreProcessing(string filename)
		{
			bool success = SourceControlSync.DeletePreProcessing(filename);
			if (success) return true;

			throw new Exception("[Rainbow] Delete pre-processing failed for " + filename);
		}
	}
}