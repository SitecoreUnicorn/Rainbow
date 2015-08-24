using System;
using Rainbow.Formatting.FieldFormatters;
using Rainbow.Model;

namespace Rainbow.Storage.Yaml.OutputModel
{
	public class YamlFieldValue : IComparable<YamlFieldValue>
	{
		public Guid Id { get; set; }
		public string NameHint { get; set; }
		public string Type { get; set; }
		public string Value { get; set; }
		public Guid? BlobId { get; set; }

		public int CompareTo(YamlFieldValue other)
		{
			return Id.CompareTo(other.Id);
		}

		public void LoadFrom(IItemFieldValue field, IFieldFormatter[] formatters)
		{
			Id = field.FieldId;
			NameHint = field.NameHint;
			BlobId = field.BlobId;
			
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

			if(BlobId.HasValue)
				writer.WriteMap("BlobID", BlobId.Value.ToString("D"));
			
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

			var blob = reader.PeekMap();
			if (blob.HasValue && blob.Value.Key.Equals("BlobID"))
			{
				BlobId = reader.ReadExpectedGuidMap("BlobID");
			}

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
