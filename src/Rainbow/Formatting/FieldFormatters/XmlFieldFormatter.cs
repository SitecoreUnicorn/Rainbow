using System;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Rainbow.Model;

namespace Rainbow.Formatting.FieldFormatters
{
	public class XmlFieldFormatter : FieldTypeBasedFormatter
	{
		public override string[] SupportedFieldTypes
		{
			get { return new[] { "Layout", "Tracking", "Rules" }; }
		}

		public override string Format(IItemFieldValue field)
		{
			try
			{
				XDocument doc = XDocument.Parse(field.Value);
				return doc.ToString();
			}
			catch (Exception)
			{
				return field.Value;
			}
		}

		public override string Unformat(string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return null;

			var stringBuilder = new StringBuilder();

			var element = XElement.Parse(value);

			var settings = new XmlWriterSettings
			{
				OmitXmlDeclaration = true,
				Indent = false,
				NewLineOnAttributes = false
			};

			using (var xmlWriter = XmlWriter.Create(stringBuilder, settings))
			{
				element.Save(xmlWriter);
			}

			return stringBuilder.ToString();
		}
	}
}
