using System;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Rainbow.Model;
using Sitecore.Configuration;
using Sitecore.Diagnostics;

namespace Rainbow.Formatting.FieldFormatters
{
	public class XmlFieldFormatter : FieldTypeBasedFormatter
	{
		public override string[] SupportedFieldTypes => new[] { "Layout", "Tracking", "Rules" };

		protected virtual bool UseLegacyFormatting => Settings.GetBoolSetting("Rainbow.XmlFormat.UseLegacyAttributeFormatting", false);

		public override string Format(IItemFieldValue field)
		{
			if (string.IsNullOrWhiteSpace(field.Value)) return field.Value;

			try
			{
				XDocument doc = XDocument.Parse(field.Value);
				XmlWriterSettings settings = new XmlWriterSettings
				{
					OmitXmlDeclaration = true,
					Indent = true,
					NewLineOnAttributes = !UseLegacyFormatting,
				};

				StringBuilder result = new StringBuilder();

				using (XmlWriter writer = XmlWriter.Create(result, settings))
				{
					doc.WriteTo(writer);
				}

				return result.ToString();
			}
			catch (Exception ex)
			{
				Log.Error($"Error while formatting XML field {field.FieldId} ({field.NameHint}). The raw value will be used instead. Raw value was {field.Value}", ex, this);
				return field.Value;
			}
		}

		public override string Unformat(string value)
		{
			if (value == null) return null;
			if (string.IsNullOrWhiteSpace(value)) return value;

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
				Log.Error($"Error while unformatting XML field value '{value}'. The raw value will be used instead.", ex, this);
				return value;
			}
		}
	}
}
