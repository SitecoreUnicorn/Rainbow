using System;
using System.Collections.Generic;
using Gibson.Data.FieldFormatters;
using Gibson.Model;

namespace Gibson.Data.Json
{
	public class JsonVersion : IComparable<JsonVersion>
	{
		public JsonVersion()
		{
			Fields = new SortedSet<JsonFieldValue>();
		}

		public int VersionNumber { get; set; }
		public SortedSet<JsonFieldValue> Fields { get; private set; } 
		
		public int CompareTo(JsonVersion other)
		{
			return VersionNumber.CompareTo(other.VersionNumber);
		}

		public void LoadFrom(ISerializableVersion version, IFieldFormatter[] fieldFormatters)
		{
			VersionNumber = version.VersionNumber;

			foreach (var field in version.Fields)
			{
				var fieldObject = new JsonFieldValue();

				fieldObject.LoadFrom(field, fieldFormatters);

				Fields.Add(fieldObject);
			}
		}
	}
}
