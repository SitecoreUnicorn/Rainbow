using System;
using System.Collections.Generic;
using Gibson.Model;
using Gibson.SerializationFormatting.FieldFormatters;

namespace Gibson.SerializationFormatting.Json
{
	public class JsonLanguage : IComparable<JsonLanguage>
	{
		public JsonLanguage()
		{
			Versions = new SortedSet<JsonVersion>();
		}

		public string Language { get; set; }
		public SortedSet<JsonVersion> Versions { get; private set; }

		public int CompareTo(JsonLanguage other)
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

				var versionObject = new JsonVersion();
				versionObject.LoadFrom(version, fieldFormatters);

				Versions.Add(versionObject);
			}
		}
	}
}
