using Rainbow.Model;

namespace Rainbow.Diff.Fields
{
	public class BlobComparisonIgnorer : FieldTypeBasedComparer
	{
		public override bool AreEqual(ISerializableFieldValue field1, ISerializableFieldValue field2)
		{
			// we ignore comparing blob fields and treat them as always 'equal'
			// TODO: unicorn depends on the presence of metadata fields (e.g. updated, revision) to 'diff' binaries
			// TODO: but, if we do not diff those fields or store them any longer then we have no binary diffing without full comparison...
			// TODO: so maybe introduce the idea of inconsequential changes into the comparer and use that
			return true;
		}

		public override string[] SupportedFieldTypes
		{
			get { return new[] { "attachment" }; }
		}
	}
}
