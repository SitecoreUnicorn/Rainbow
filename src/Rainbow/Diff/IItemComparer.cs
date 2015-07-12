using Rainbow.Model;

namespace Rainbow.Diff
{
	public interface IItemComparer
	{
		ItemComparisonResult Compare(IItemData sourceItem, IItemData targetItem);
		bool SimpleCompare(IItemData sourceItem, IItemData targetItem);
	}
}
