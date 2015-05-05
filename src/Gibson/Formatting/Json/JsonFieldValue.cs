using System;

namespace Gibson.Formatting.Json
{
	public class JsonFieldValue : IComparable<JsonFieldValue>
	{
		public Guid Id { get; set; }
		public string Type { get; set; }
		public string Value { get; set; }

		public int CompareTo(JsonFieldValue other)
		{
			return Id.CompareTo(other.Id);
		}
	}
}
