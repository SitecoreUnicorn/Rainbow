using System;
using System.Linq;
using Rainbow.Formatting.FieldFormatters;
using Rainbow.Model;
using Sitecore.Diagnostics;

namespace Rainbow.Formatting
{
	public abstract class FieldTypeBasedFormatter : IFieldFormatter
	{
		public abstract string[] SupportedFieldTypes { get; }
		public bool CanFormat(IItemFieldValue field)
		{
			Assert.ArgumentNotNull(field, "field");

			if (field.FieldType == null) return false;

			return SupportedFieldTypes.Any(type => type.Equals(field.FieldType, StringComparison.OrdinalIgnoreCase));
		}

		public abstract string Format(IItemFieldValue field);

		public abstract string Unformat(string value);
	}
}
