using Sitecore.Common;

namespace Rainbow.Storage
{
	/// <summary>
	/// Normally while reading items, SFS checks its metadata cache for inconsistencies automatically
	/// and throws an exception if it detects two items on disk that have the same item ID, as that indicates corruption
	/// 
	/// However when we move or rename an item, we write the moved/renamed item(s) prior to deleting the old location's data
	/// This means temporarily there are dupe IDs that we expect, and this disabler lets us successfully delete the old stuff
	/// without throwing the duplicate exception.
	/// </summary>
	public class SfsDuplicateIdCheckingDisabler : Switcher<bool, SfsDuplicateIdCheckingDisabler>
	{
		public SfsDuplicateIdCheckingDisabler() : base(true)
		{

		}
	}
}
