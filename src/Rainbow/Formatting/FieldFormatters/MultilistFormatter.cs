using System;
using System.Collections.Generic;
using Rainbow.Model;
using Sitecore.Data;

namespace Rainbow.Formatting.FieldFormatters
{
	public class MultilistFormatter : FieldTypeBasedFormatter
	{
		public override string[] SupportedFieldTypes
		{
			get
			{
				return new[] { "Checklist", "Multilist", "Multilist with Search", "Treelist", "Treelist with Search", "TreelistEx" };
			}
		}

		public override string Format(IItemFieldValue field)
		{
			var values = ID.ParseArray(field.Value);

			if (values.Length == 0 && field.Value.Length > 0)
				return field.Value;

			return string.Join(Environment.NewLine, (IEnumerable<ID>)values);
		}

		public override string Unformat(string value)
		{
			if (value == null) return null;
			return value.Trim().Replace(Environment.NewLine, "|");
		}
	}
}
