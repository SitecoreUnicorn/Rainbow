using System;
using Rainbow.Model;

namespace Rainbow.Diff.Fields
{
	public class DefaultComparison : IFieldComparer
	{
		public virtual bool CanCompare(IItemFieldValue field1, IItemFieldValue field2)
		{
			return field1 != null && field2 != null;
		}

		public virtual bool AreEqual(IItemFieldValue field1, IItemFieldValue field2)
		{
			if (field1.BlobId.HasValue && field2.BlobId.HasValue) return field1.BlobId.Value.Equals(field2.BlobId.Value);

			var field1Value = field1.Value;
			var field2Value = field2.Value;

			if (field1Value == null || field2Value == null) return false;

			return field1Value.Equals(field2Value, StringComparison.Ordinal);
		}
	}
}
