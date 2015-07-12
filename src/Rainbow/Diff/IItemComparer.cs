using Rainbow.Model;

namespace Rainbow.Diff
{
	public interface IItemComparer
	{
		ItemComparisonResult Compare(IItemData targetItemData, IItemData sourceItemData);
		bool SimpleCompare(IItemData targetItemData, IItemData sourceItemData);
	}
}
