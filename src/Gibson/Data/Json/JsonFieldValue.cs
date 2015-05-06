using System;
using Gibson.Data.FieldFormatters;
using Gibson.Model;

namespace Gibson.Data.Json
{
	public class JsonFieldValue : IComparable<JsonFieldValue>
	{
		public Guid Id { get; set; }
		public string Type { get; set; }
		public string Value { get; set; }

		public int CompareTo(JsonFieldValue other)
		{
			return Id.CompareTo(other.Id);
		}

		public void LoadFrom(ISerializableFieldValue field, IFieldFormatter[] formatters)
		{
			Id = field.FieldId;
			Type = field.FieldType;

			string value = field.Value;

			foreach(var formatter in formatters)
			{
				if (formatter.CanFormat(field))
				{
					value = formatter.Format(field);
					break;
				}
			}

			Value = value;
		}
	}
}
