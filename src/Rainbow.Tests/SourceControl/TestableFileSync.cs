using Rainbow.SourceControl;

namespace Rainbow.Tests.SourceControl
{
	public class TestableFileSync : ISourceControlSync
	{
		public bool AllowFileSystemClear { get; }
		public bool Result { get; }

		public TestableFileSync(bool result)
		{
			Result = result;
		}

		public bool DeletePreProcessing(string filename)
		{
			return Result;
		}

		public bool EditPreProcessing(string filename)
		{
			return Result;
		}

		public bool EditPostProcessing(string filename)
		{
			return Result;
		}
	}
}