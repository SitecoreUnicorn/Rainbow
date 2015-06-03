using System;
using System.Collections.Generic;
using Gibson.Model;
using Gibson.SerializationFormatting.FieldFormatters;

namespace Gibson.SerializationFormatting.Yaml
{
	public class YamlLanguage : IComparable<YamlLanguage>
	{
		public YamlLanguage()
		{
			Versions = new SortedSet<YamlVersion>();
		}

		public string Language { get; set; }
		public SortedSet<YamlVersion> Versions { get; private set; }

		public int CompareTo(YamlLanguage other)
		{
			return String.Compare(Language, other.Language, StringComparison.Ordinal);
		}

		public void LoadFrom(IEnumerable<ISerializableVersion> versions, IFieldFormatter[] fieldFormatters)
		{
			bool first = true;
			foreach (var version in versions)
			{
				if (first)
				{
					Language = version.Language.Name;
					first = false;
				}

				var versionObject = new YamlVersion();
				versionObject.LoadFrom(version, fieldFormatters);

				Versions.Add(versionObject);
			}
		}

		public void WriteYaml(YamlWriter writer)
		{
			writer.WriteBeginListItem("Language", Language);
			writer.WriteMap("Versions");

			writer.IncreaseIndent();

			foreach (var version in Versions)
			{
				version.WriteYaml(writer);
			}

			writer.DecreaseIndent();
		}

		public bool ReadYaml(YamlReader reader)
		{
			var language = reader.PeekMap();
			if (!language.Key.Equals("Language", StringComparison.Ordinal)) return false;

			Language = reader.ReadExpectedMap("Language");
			reader.ReadExpectedMap("Versions");

			while (true)
			{
				var version = new YamlVersion();
				if (version.ReadYaml(reader)) Versions.Add(version);
				else break;
			}

			return true;
		}
	}
}
