using System.Collections.Generic;
using Rainbow.Model;

namespace Rainbow.Storage
{
	public interface ISnapshotCapableDataStore : IDataStore
	{
		/// <summary>
		/// Returns every single item in the whole data store. This method is used for data stores where
		/// getting every single item is faster than children traversal with GetChildren()
		/// e.g. with SFS, parallel flat reading of the files over 18k items was ~1.7sec vs children traversal ~5-6sec.
		/// Note that the consumer of this is responsible for indexing the items for actual usage. And don't use this for multi-GB
		/// media configurations for obvious reasons - unless you have GBs of RAM lying around ;)
		/// </summary>
		IEnumerable<IItemData> GetSnapshot();
	}
}
