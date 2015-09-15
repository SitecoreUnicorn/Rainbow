using System;

namespace Rainbow.SourceControl
{
	public class SourceControlManager : ISourceControlManager
	{
		private readonly ISourceControlSync _sourceControlSync;

		public SourceControlManager(string filename)
		{
			_sourceControlSync = Activator.CreateInstance(typeof(TfsFileSync), filename) as ISourceControlSync;
		}

		public bool Edit()
		{
			return _sourceControlSync.CheckoutFile();
		}

		public bool Add()
		{
			return _sourceControlSync.AddFile();
		}

		public bool Remove()
		{
			return _sourceControlSync.CheckoutFile();
		}
	}
}