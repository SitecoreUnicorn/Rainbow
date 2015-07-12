using System;
using System.Collections.Generic;
using Rainbow.Model;
using Sitecore.Data;

namespace Rainbow.Formatting.FieldFormatters
{
	public class MultilistFormatter : IFieldFormatter
	{
		public virtual bool CanFormat(IItemFieldValue field)
		{
			if (field.FieldType == null) return false;

			return field.FieldType.Equals("Checklist", StringComparison.OrdinalIgnoreCase) ||
				   field.FieldType.Equals("Multilist", StringComparison.OrdinalIgnoreCase) ||
				   field.FieldType.Equals("Multilist with Search", StringComparison.OrdinalIgnoreCase) ||
				   field.FieldType.Equals("Treelist", StringComparison.OrdinalIgnoreCase) ||
				   field.FieldType.Equals("Treelist with Search", StringComparison.OrdinalIgnoreCase) ||
				   field.FieldType.Equals("TreelistEx", StringComparison.OrdinalIgnoreCase);
		}

		public virtual string Format(IItemFieldValue field)
		{
			var values = ID.ParseArray(field.Value);

			return string.Join(Environment.NewLine, (IEnumerable<ID>)values);
		}

		public virtual string Unformat(string value)
		{
			if (value == null) return null;
			return value.Trim().Replace(Environment.NewLine, "|");
		}
	}
}
