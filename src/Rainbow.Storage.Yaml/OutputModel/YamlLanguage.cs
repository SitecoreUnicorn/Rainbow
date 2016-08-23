using System;
using System.Collections.Generic;
using Rainbow.Formatting.FieldFormatters;
using Rainbow.Model;

namespace Rainbow.Storage.Yaml.OutputModel
{
	public class YamlLanguage : IComparable<YamlLanguage>
	{
		public YamlLanguage()
		{
			Versions = new SortedSet<YamlVersion>();
			UnversionedFields = new SortedSet<YamlFieldValue>();
		}

		public string Language { get; set; }

		public SortedSet<YamlVersion> Versions { get; }

		public SortedSet<YamlFieldValue> UnversionedFields { get; }

		public int CompareTo(YamlLanguage other)
		{
			return string.Compare(Language, other.Language, StringComparison.Ordinal);
		}

		public void LoadFrom(IEnumerable<IItemVersion> versions, IFieldFormatter[] fieldFormatters)
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
			// at times we may receive an empty language with no fields or versions
			// we avoid writing it the output, as it's irrelevant
			if (UnversionedFields.Count == 0 && Versions.Count == 0) return;

			writer.WriteBeginListItem("Language", Language);

			if (UnversionedFields.Count > 0)
			{
				writer.WriteMap("Fields");
				writer.IncreaseIndent();

				foreach (var unversionedField in UnversionedFields)
				{
					unversionedField.WriteYaml(writer);
				}

				writer.DecreaseIndent();
			}

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
			if (!language.HasValue || !language.Value.Key.Equals("Language", StringComparison.Ordinal)) return false;

			Language = reader.ReadExpectedMap("Language");

			var fields = reader.PeekMap();
			if (fields.HasValue && fields.Value.Key.Equals("Fields", StringComparison.OrdinalIgnoreCase))
			{
				reader.ReadExpectedMap("Fields");
				while (true)
				{
					var field = new YamlFieldValue();
					if (field.ReadYaml(reader)) UnversionedFields.Add(field);
					else break;
				}
			}

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
