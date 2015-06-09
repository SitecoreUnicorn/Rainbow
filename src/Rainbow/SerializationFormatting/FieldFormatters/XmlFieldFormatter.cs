using System;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Rainbow.Model;

namespace Rainbow.SerializationFormatting.FieldFormatters
{
	public class XmlFieldFormatter : IFieldFormatter
	{
		public virtual bool CanFormat(ISerializableFieldValue field)
		{
			if (field.FieldType == null) return false;

			return field.FieldType.Equals("Layout", StringComparison.OrdinalIgnoreCase);
		}

		public virtual string Format(ISerializableFieldValue field)
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

		public virtual string Unformat(string value)
		{
			if (value == null) return null;

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
