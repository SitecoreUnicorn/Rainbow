using System;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Gibson.Model;

namespace Gibson.SerializationFormatting.FieldFormatters
{
	public class XmlFieldFormatter : IFieldFormatter
	{
		public bool CanFormat(ISerializableFieldValue field)
		{
			return field.FieldType.Equals("Layout");
		}

		public string Format(ISerializableFieldValue field)
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

		public string Unformat(string value)
		{
			var stringBuilder = new StringBuilder();

			var element = XElement.Parse(value);

			var settings = new XmlWriterSettings();
			settings.OmitXmlDeclaration = true;
			settings.Indent = false;
			settings.NewLineOnAttributes = false;

			using (var xmlWriter = XmlWriter.Create(stringBuilder, settings))
			{
				element.Save(xmlWriter);
			}

			return stringBuilder.ToString();
		}
	}
}
