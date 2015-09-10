using System.Collections.Generic;

namespace Rainbow.Storage
{
	/// <summary>
	/// Represents an object that can tell the SFS where it should create tree roots
	/// (e.g. what subtrees of a complete tree we're storing in SFS)
	/// </summary>
	public interface ITreeRootFactory
	{
		IEnumerable<TreeRoot> CreateTreeRoots();
	}
}
