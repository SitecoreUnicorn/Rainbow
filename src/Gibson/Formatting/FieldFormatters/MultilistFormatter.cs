using System;
using System.Collections.Generic;
using Gibson.Model;
using Sitecore.Data;

namespace Gibson.Formatting.FieldFormatters
{
	public class MultilistFormatter : IFieldFormatter
	{
		public bool CanFormat(ISerializableFieldValue field)
		{
			return field.FieldType.Equals("Checklist") ||
				   field.FieldType.Equals("Multilist") ||
				   field.FieldType.Equals("Multilist with Search") ||
				   field.FieldType.Equals("Treelist") ||
				   field.FieldType.Equals("Treelist with Search") ||
				   field.FieldType.Equals("TreelistEx");
		}

		public string Format(ISerializableFieldValue field)
		{
			var values = ID.ParseArray(field.Value);

			return string.Join(Environment.NewLine, (IEnumerable<ID>)values);
		}

		public string Unformat(string value)
		{
			return value.Trim().Replace(Environment.NewLine, "|");
		}
	}
}
