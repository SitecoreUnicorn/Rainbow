using System;
using System.Collections.Generic;
using System.Linq;
using Rainbow.Model;

namespace Rainbow.Diff.Fields
{
	public abstract class FieldTypeBasedComparison : IFieldComparer
	{
		protected HashSet<string> FieldTypeLookup;
		protected object SyncLock = new object();
		 
		public virtual bool CanCompare(IItemFieldValue field1, IItemFieldValue field2)
		{
			if (FieldTypeLookup == null)
			{
				lock (SyncLock)
				{
					if (FieldTypeLookup == null)
					{
						FieldTypeLookup = new HashSet<string>(SupportedFieldTypes, StringComparer.OrdinalIgnoreCase);
					}
				}
			}

			var typeToCompare = field1.FieldType ?? field2.FieldType;

			if(typeToCompare == null) throw new Exception("Cannot compare two fields without a type.");

			return FieldTypeLookup.Contains(typeToCompare);
		}

		public abstract bool AreEqual(IItemFieldValue field1, IItemFieldValue field2);

		public abstract string[] SupportedFieldTypes { get; }
	}
}
