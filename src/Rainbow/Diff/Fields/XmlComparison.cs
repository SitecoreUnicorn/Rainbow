using System.Xml;
using System.Xml.Linq;
using Rainbow.Model;
using Sitecore.Diagnostics;

namespace Rainbow.Diff.Fields
{
	public class XmlComparison : FieldTypeBasedComparison
	{
		public override bool AreEqual(IItemFieldValue field1, IItemFieldValue field2)
		{
			if (string.IsNullOrWhiteSpace(field1.Value) && string.IsNullOrWhiteSpace(field2.Value)) return true;

			if (string.IsNullOrWhiteSpace(field1.Value) || string.IsNullOrWhiteSpace(field2.Value)) return false;

			try
			{
				var x1 = XElement.Parse(field1.Value);
				var x2 = XElement.Parse(field2.Value);

				return XNode.DeepEquals(x1, x2);
			}
			catch (XmlException xe)
			{
				Log.Error($"Field {field1.NameHint ?? field2.NameHint} contained an invalid XML value. Falling back to string comparison.", xe, this);

				return new DefaultComparison().AreEqual(field1, field2);
			}
		}

		public override string[] SupportedFieldTypes => new[] { "Layout", "Tracking", "Rules" };
	}
}
