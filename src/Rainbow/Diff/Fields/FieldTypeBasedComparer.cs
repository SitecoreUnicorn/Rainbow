using System;
using System.Linq;
using Rainbow.Model;

namespace Rainbow.Diff.Fields
{
	public abstract class FieldTypeBasedComparer : IFieldComparer
	{
		public bool CanCompare(IItemFieldValue field1, IItemFieldValue field2)
		{
			var typeToCompare = field1.FieldType ?? field2.FieldType;

			if(typeToCompare == null) throw new Exception("Cannot compare two fields without a type.");

			return SupportedFieldTypes.Any(type => type.Equals(typeToCompare, StringComparison.OrdinalIgnoreCase));
		}

		public abstract bool AreEqual(IItemFieldValue field1, IItemFieldValue field2);

		public abstract string[] SupportedFieldTypes { get; }
	}
}
