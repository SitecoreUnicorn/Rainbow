using System.Xml.Linq;
using Rainbow.Model;

namespace Rainbow.Diff.Fields
{
	public class XmlComparison : FieldTypeBasedComparer
	{
		public override bool AreEqual(ISerializableFieldValue field1, ISerializableFieldValue field2)
		{
			if (string.IsNullOrWhiteSpace(field1.Value) || string.IsNullOrWhiteSpace(field2.Value)) return false;

			var x1 = XElement.Parse(field1.Value);
			var x2 = XElement.Parse(field2.Value);

			return XNode.DeepEquals(x1, x2);
		}

		public override string[] SupportedFieldTypes
		{
			get { return new[] { "Layout" }; }
		}
	}
}
