using System;
using System.Collections.Generic;
using Rainbow.Model;
using Sitecore.Diagnostics;

namespace Rainbow.Formatting.FieldFormatters
{
	public abstract class FieldTypeBasedFormatter : IFieldFormatter
	{
		private HashSet<string> _fieldTypeSet;
		 
		public abstract string[] SupportedFieldTypes { get; }

		public bool CanFormat(IItemFieldValue field)
		{
			Assert.ArgumentNotNull(field, "field");

			if (field.FieldType == null) return false;

			if(_fieldTypeSet == null) _fieldTypeSet = new HashSet<string>(SupportedFieldTypes, StringComparer.OrdinalIgnoreCase);

			return _fieldTypeSet.Contains(field.FieldType);
		}

		public abstract string Format(IItemFieldValue field);

		public abstract string Unformat(string value);
	}
}
