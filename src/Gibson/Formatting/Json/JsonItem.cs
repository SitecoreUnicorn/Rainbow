using System;
using System.Collections.Generic;

namespace Gibson.Formatting.Json
{
	public class JsonItem
	{
		public JsonItem()
		{
			SharedFields = new SortedSet<JsonFieldValue>();
			Languages = new SortedSet<JsonLanguage>();
		}

		public Guid Id { get; set; }
		public string DatabaseName { get; set; }
		public string Name { get; set; }
		public Guid BranchId { get; set; }
		public SortedSet<JsonFieldValue> SharedFields { get; private set; }
		public SortedSet<JsonLanguage> Languages { get; private set; }
	}
}
