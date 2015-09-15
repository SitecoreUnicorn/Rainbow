using System;

namespace Rainbow.SourceControl
{
	public interface ISourceControlManager
	{
		IDisposable GetSourceControlManager(string filename);
	}
}