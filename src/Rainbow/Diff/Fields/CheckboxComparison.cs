using System;
using Rainbow.Formatting.FieldFormatters;
using Rainbow.Model;

namespace Rainbow.Diff.Fields
{
	/// <summary>
	/// Comparer for checkboxes. Normalizes out differences between 0, 1, blank, true, etc
	/// </summary>
	public class CheckboxComparison : FieldTypeBasedComparison
	{
		public override bool AreEqual(IItemFieldValue field1, IItemFieldValue field2)
		{
			if (field1.Value == null || field2.Value == null) return false;

			var formatter = new CheckboxFieldFormatter();

			string v1 = formatter.Format(field1);
			string v2 = formatter.Format(field2);

			return v1.Equals(v2, StringComparison.Ordinal);
		}

		public override string[] SupportedFieldTypes => new[] { "Checkbox" };
	}
}
