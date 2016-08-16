using System;
using Rainbow.Model;

namespace Rainbow.Formatting.FieldFormatters
{
	/// <summary>
	///     Formats checkboxes as explicit values of "0" or "1"
	///     Normally Sitecore stores false as a blank value. This is bad because it can cause havoc with blanks
	///     versus standard values. So to disambiguate this, we force false to zero.
	///     This DOES mean that a sync after reserializing checkbox fields will detect a 'change' as the value
	///     in Sitecore is normalized to zero.
	/// </summary>
	public class CheckboxFieldFormatter : FieldTypeBasedFormatter
	{
		public override string[] SupportedFieldTypes => new[] {"Checkbox"};

		public override string Format(IItemFieldValue field)
		{
			if (field.Value.Equals("1", StringComparison.Ordinal) || field.Value.Equals("true", StringComparison.OrdinalIgnoreCase)) return "1";

			return "0";
		}

		public override string Unformat(string value)
		{
			if (value.Equals("0", StringComparison.Ordinal))
				return string.Empty;
			return value;
		}
	}
}