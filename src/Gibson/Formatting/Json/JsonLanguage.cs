using System;
using System.Collections.Generic;

namespace Gibson.Formatting.Json
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
	}
}
