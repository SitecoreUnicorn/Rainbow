using System;

namespace Rainbow.SourceControl
{
	public class SourceControlManager : ISourceControlManager
	{
		private readonly ISourceControlSync _sourceControlSync;

		public bool AllowFileSystemClear { get { return _sourceControlSync.AllowFileSystemClear; } }

		public SourceControlManager(ISourceControlSync sourceControlSync)
		{
			_sourceControlSync = sourceControlSync;
		}

		public bool EditPreProcessing(string filename)
		{
			bool success = _sourceControlSync.EditPreProcessing(filename);
			if (success) return true;

			throw new Exception("[Rainbow] Edit pre-processing failed for " + filename);
		}

		public bool EditPostProcessing(string filename)
		{
			bool success = _sourceControlSync.EditPostProcessing(filename);
			if (success) return true;

			throw new Exception("[Rainbow] Edit post-processing failed for " + filename);
		}

		public bool DeletePreProcessing(string filename)
		{
			bool success = _sourceControlSync.DeletePreProcessing(filename);
			if (success) return true;

			throw new Exception("[Rainbow] Delete pre-processing failed for " + filename);
		}
	}
}