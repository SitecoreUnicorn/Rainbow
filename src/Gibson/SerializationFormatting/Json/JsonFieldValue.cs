using System;
using Gibson.Model;
using Gibson.SerializationFormatting.FieldFormatters;
using Newtonsoft.Json;

namespace Gibson.SerializationFormatting.Json
{
	public class JsonFieldValue : IComparable<JsonFieldValue>
	{
		public Guid Id { get; set; }
		public string NameHint { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string Type { get; set; }
		public string Value { get; set; }

		public int CompareTo(JsonFieldValue other)
		{
			return Id.CompareTo(other.Id);
		}

		public void LoadFrom(ISerializableFieldValue field, IFieldFormatter[] formatters)
		{
			Id = field.FieldId;
			NameHint = field.NameHint;
			
			string value = field.Value;

			foreach(var formatter in formatters)
			{
				if (formatter.CanFormat(field))
				{
					value = formatter.Format(field);
					Type = field.FieldType;

					break;
				}
			}

			Value = value;
		}
	}
}
