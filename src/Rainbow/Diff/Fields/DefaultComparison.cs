using Rainbow.Model;

namespace Rainbow.Diff.Fields
{
	public class DefaultComparison : IFieldComparer
	{
		public bool CanCompare(ISerializableFieldValue field1, ISerializableFieldValue field2)
		{
			return field1 != null && field2 != null;
		}

		public bool AreEqual(ISerializableFieldValue field1, ISerializableFieldValue field2)
		{
			if (field1.Value == null || field2.Value == null) return false;

			return field1.Value.Equals(field2.Value);
		}
	}
}
