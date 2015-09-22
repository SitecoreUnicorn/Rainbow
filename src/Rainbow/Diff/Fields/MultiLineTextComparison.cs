using System.Text.RegularExpressions;
using Rainbow.Model;

namespace Rainbow.Diff.Fields
{
	/// <summary>
	/// Comparer for multi line text fields and rich text fields. Ignores line ending differences.
	/// </summary>
	public class MultiLineTextComparison : FieldTypeBasedComparison
	{
		public override bool AreEqual(IItemFieldValue field1, IItemFieldValue field2)
		{
			if (field1.Value == null || field2.Value == null) return false;

			string v1 = Regex.Replace(field1.Value, "(\r|\n)+", string.Empty);
			string v2 = Regex.Replace(field2.Value, "(\r|\n)+", string.Empty);

			return v1.Equals(v2);
		}

		public override string[] SupportedFieldTypes
		{
			get { return new[] { "Multi-Line Text", "Rich Text", "html", "memo" }; }
		}
	}
}
