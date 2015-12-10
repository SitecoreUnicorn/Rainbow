using System;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Rainbow.Model;
using Sitecore.Diagnostics;

namespace Rainbow.Formatting.FieldFormatters
{
	public class XmlFieldFormatter : FieldTypeBasedFormatter
	{
		public override string[] SupportedFieldTypes => new[] { "Layout", "Tracking", "Rules" };

		public override string Format(IItemFieldValue field)
		{
			try
			{
				XDocument doc = XDocument.Parse(field.Value);
				return doc.ToString();
			}
			catch (Exception ex)
			{
				Log.Error($"Error while formatting XML field {field.FieldId} ({field.NameHint}). The raw value will be used instead.", ex, this);
				return field.Value;
			}
		}

		public override string Unformat(string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return null;

			try
			{
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
			catch (Exception ex)
			{
				Log.Error($"Error while unformatting expected XML field {value}. The raw value will be used instead.", ex, this);
				return value;
			}
		}
	}
}
