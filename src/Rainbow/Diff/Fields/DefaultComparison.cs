using Rainbow.Model;

namespace Rainbow.Diff.Fields
{
	public class DefaultComparison : IFieldComparer
	{
		public bool CanCompare(IItemFieldValue field1, IItemFieldValue field2)
		{
			return field1 != null && field2 != null;
		}

		public bool AreEqual(IItemFieldValue field1, IItemFieldValue field2)
		{
			if (field1.Value == null || field2.Value == null) return false;

			if (field1.BlobId.HasValue && field2.BlobId.HasValue) return field1.BlobId.Value.Equals(field2.BlobId.Value);

			return field1.Value.Equals(field2.Value);
		}
	}
}
