using Rainbow.SourceControl;

namespace Rainbow.Tests.SourceControl
{
	public class TestableTfsFileSync : ISourceControlSync
	{
		private readonly bool _result;

		public TestableTfsFileSync(bool result)
		{
			_result = result;
		}

		public bool AllowFileSystemClear { get; private set; }
		public bool DeletePreProcessing(string filename)
		{
			return _result;
		}

		public bool EditPreProcessing(string filename)
		{
			return _result;
		}

		public bool EditPostProcessing(string filename)
		{
			return _result;
		}
	}
}