using System;

namespace Rainbow.SourceControl
{
	public interface ISourceControlManager
	{
		bool Edit();
		bool Add();
		bool Remove();
	}
}