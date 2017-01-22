using System.Text;
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

			var v1 = StripEndlines(field1.Value);
			var v2 = StripEndlines(field2.Value);

			return v1.Equals(v2);
		}

		protected virtual string StripEndlines(string value)
		{
			var valueReplacer = new StringBuilder(value);
			valueReplacer.Replace("\r", string.Empty);
			valueReplacer.Replace("\n", string.Empty);

			return valueReplacer.ToString();
		}

		public override string[] SupportedFieldTypes => new[] { "Multi-Line Text", "Rich Text", "html", "memo" };
	}
}
