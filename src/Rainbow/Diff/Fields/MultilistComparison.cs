using Rainbow.Model;

namespace Rainbow.Diff.Fields
{
	public class MultilistComparison : FieldTypeBasedComparison
	{
		public override bool AreEqual(IItemFieldValue field1, IItemFieldValue field2)
		{
			var field1Value = field1.Value;
			var field2Value = field2.Value;

			if (field1Value == null || field2Value == null) return false;

			// certain Sitecore multilists - I'm looking at YOU __Masters - seem to like to add a trailing pipe to their values
			// this can cause a sync right after a reserialize to have 'changes' as Unicorn fixes Sitecore's problems!
			// so to avoid confusion, we'll just ignore leading or trailing pipes when comparing a multilist
			return field1Value.Trim('|').Equals(field2Value.Trim('|'));
		}

		public override string[] SupportedFieldTypes
		{
			get
			{
				return new[] { "Checklist", "Multilist", "Multilist with Search", "Treelist", "Treelist with Search", "TreelistEx", "tree list" };
			}
		}
	}
}
