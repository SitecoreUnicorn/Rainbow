using Rainbow.Model;

namespace Rainbow.Diff
{
	public interface IItemComparer
	{
		ItemComparisonResult Compare(IItemData sourceItem, IItemData targetItem);
		ItemComparisonResult FastCompare(IItemData sourceItem, IItemData targetItem);
	}
}
