using Rainbow.Model;

namespace Rainbow.Diff
{
	public interface IItemComparer
	{
		ItemComparisonResult Compare(ISerializableItem targetItem, ISerializableItem sourceItem);
	}
}
