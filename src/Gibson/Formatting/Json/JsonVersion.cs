using System;
using System.Collections.Generic;

namespace Gibson.Formatting.Json
{
	public class JsonVersion : IComparable<JsonVersion>
	{
		public JsonVersion()
		{
			Fields = new SortedSet<JsonFieldValue>();
		}

		public int VersionNumber { get; set; }
		public SortedSet<JsonFieldValue> Fields { get; private set; } 
		
		public int CompareTo(JsonVersion other)
		{
			return VersionNumber.CompareTo(other.VersionNumber);
		}
	}
}
