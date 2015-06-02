using System;
using System.Collections.Generic;
using System.Linq;
using Gibson.Model;
using Gibson.SerializationFormatting.FieldFormatters;

namespace Gibson.SerializationFormatting.Yaml
{
	public class YamlVersion : IComparable<YamlVersion>
	{
		public YamlVersion()
		{
			Fields = new SortedSet<YamlFieldValue>();
		}

		public int VersionNumber { get; set; }
		public SortedSet<YamlFieldValue> Fields { get; private set; } 
		
		public int CompareTo(YamlVersion other)
		{
			return VersionNumber.CompareTo(other.VersionNumber);
		}

		public void LoadFrom(ISerializableVersion version, IFieldFormatter[] fieldFormatters)
		{
			VersionNumber = version.VersionNumber;

			foreach (var field in version.Fields)
			{
				if (string.IsNullOrWhiteSpace(field.Value)) continue;

				var fieldObject = new YamlFieldValue();

				fieldObject.LoadFrom(field, fieldFormatters);

				Fields.Add(fieldObject);
			}
		}

		public void WriteYaml(YamlWriter writer)
		{
			writer.WriteBeginListItem("Version", VersionNumber.ToString());

			if (Fields.Any())
			{
				writer.WriteMap("Fields");
				writer.IncreaseIndent();

				foreach (var field in Fields)
				{
					field.WriteYaml(writer);
				}

				writer.DecreaseIndent();
			}
		}
	}
}
