using System;
using System.Diagnostics;
using Rainbow.Formatting.FieldFormatters;
using Rainbow.Model;

namespace Rainbow.Storage.Yaml.Formatting.OutputModel
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

		public bool ReadYaml(YamlReader reader)
		{
			var id = reader.PeekMap();
			if (!id.HasValue || !id.Value.Key.Equals("ID", StringComparison.Ordinal))
			{
				return false;
			}

			Id = reader.ReadExpectedGuidMap("ID");
			Debug.WriteLine("Read ID " + id);

			var type = reader.PeekMap();
			if (type.HasValue && type.Value.Key.Equals("Type"))
			{
				reader.ReadMap();
				Type = type.Value.Value;
			}

			Value = reader.ReadExpectedMap("Value");

			return true;
		}
	}
}
