using System;
using System.Collections.Generic;
using System.Linq;
using Rainbow.Formatting.FieldFormatters;
using Rainbow.Model;

namespace Rainbow.Storage.Yaml.Formatting.OutputModel
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

		public void LoadFrom(IItemVersion version, IFieldFormatter[] fieldFormatters)
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

		public virtual bool ReadYaml(YamlReader reader)
		{
			var version = reader.PeekMap();
			if (!version.HasValue || !version.Value.Key.Equals("Version", StringComparison.Ordinal)) return false;

			VersionNumber = int.Parse(reader.ReadExpectedMap("Version"));

			var fields = reader.PeekMap();
			if (fields.HasValue && fields.Value.Key.Equals("Fields", StringComparison.Ordinal))
			{
				reader.ReadMap();
				while (true)
				{
					var field = new YamlFieldValue();
					if (field.ReadYaml(reader)) Fields.Add(field);
					else break;
				}
			}

			return true;
		}
	}
}
