using Rainbow.Model;

namespace Rainbow.Diff.Fields
{
	public interface IFieldComparer
	{
		/// <remarks>The field passed may have a null type value.</remarks>
		bool CanCompare(IItemFieldValue field1, IItemFieldValue field2);

		bool AreEqual(IItemFieldValue field1, IItemFieldValue field2);
	}
}
