using System.Collections.Generic;

namespace Rainbow.Storage
{
	public interface ITreeRootFactory
	{
		IEnumerable<TreeRoot> CreateTreeRoots();
	}
}
