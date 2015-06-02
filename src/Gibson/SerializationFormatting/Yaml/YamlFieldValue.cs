using System;
using Gibson.Model;
using Gibson.SerializationFormatting.FieldFormatters;

namespace Gibson.SerializationFormatting.Yaml
{
	public class YamlFieldValue : IComparable<YamlFieldValue>
	{
		public Guid Id { get; set; }
		public string NameHint { get; set; }
		public string Type { get; set; }
		public string Value { get; set; }

		public int CompareTo(YamlFieldValue other)
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

		public void WriteYaml(YamlWriter writer)
		{
			writer.WriteBeginListItem("ID", Id.ToString("D"));

			if (!string.IsNullOrWhiteSpace(NameHint))
			{
				writer.WriteComment(NameHint);
			}
			
			if(Type != null)
				writer.WriteMap("Type", Type);
			
			writer.WriteMap("Value", Value);
		}
	}
}
