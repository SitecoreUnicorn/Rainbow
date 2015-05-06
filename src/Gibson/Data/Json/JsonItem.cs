using System;
using System.Collections.Generic;
using System.Linq;
using Gibson.Data.FieldFormatters;
using Gibson.Model;

namespace Gibson.Data.Json
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

		public void LoadFrom(ISerializableItem item, IFieldFormatter[] fieldFormatters)
		{
			Id = item.Id;
			DatabaseName = item.DatabaseName;
			Name = item.Name;
			BranchId = item.BranchId;

			foreach (var field in item.SharedFields)
			{
				var fieldObject = new JsonFieldValue();
				fieldObject.LoadFrom(field, fieldFormatters);

				SharedFields.Add(fieldObject);
			}

			var languages = item.Versions.GroupBy(x => x.Language.Name);

			foreach (var language in languages)
			{
				var languageObject = new JsonLanguage();
				languageObject.LoadFrom(language, fieldFormatters);

				if(languageObject.Versions.Count > 0)
					Languages.Add(languageObject);
			}
		}
	}
}
