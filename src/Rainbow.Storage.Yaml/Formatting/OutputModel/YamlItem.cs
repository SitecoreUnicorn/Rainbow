using System;
using System.Collections.Generic;
using System.Linq;
using Rainbow.Formatting.FieldFormatters;
using Rainbow.Model;

namespace Rainbow.Storage.Yaml.Formatting.OutputModel
{
	public class YamlItem
	{
		public YamlItem()
		{
			SharedFields = new SortedSet<YamlFieldValue>();
			Languages = new SortedSet<YamlLanguage>();
		}

		public Guid Id { get; set; }
		public Guid ParentId { get; set; }
		public Guid TemplateId { get; set; }
		public string Path { get; set; }

		public Guid BranchId { get; set; }
		public SortedSet<YamlFieldValue> SharedFields { get; private set; }
		public SortedSet<YamlLanguage> Languages { get; private set; }

		public virtual void LoadFrom(IItemData itemData, IFieldFormatter[] fieldFormatters)
		{
			Id = itemData.Id;
			ParentId = itemData.ParentId;
			TemplateId = itemData.TemplateId;
			Path = itemData.Path;

			BranchId = itemData.BranchId;

			foreach (var field in itemData.SharedFields)
			{
				if(string.IsNullOrEmpty(field.Value)) continue;

				var fieldObject = new YamlFieldValue();
				fieldObject.LoadFrom(field, fieldFormatters);

				SharedFields.Add(fieldObject);
			}

			var languages = itemData.Versions.GroupBy(x => x.Language.Name);

			foreach (var language in languages)
			{
				var languageObject = new YamlLanguage();
				languageObject.LoadFrom(language, fieldFormatters);

				if(languageObject.Versions.Count > 0)
					Languages.Add(languageObject);
			}
		}

		public virtual void WriteYaml(YamlWriter writer)
		{
			writer.WriteMap("ID", Id.ToString("D"));
			writer.WriteMap("Parent", ParentId.ToString("D"));
			writer.WriteMap("Template", TemplateId.ToString("D"));
			writer.WriteMap("Path", Path);

			if (SharedFields.Any())
			{
				writer.WriteMap("SharedFields");
				writer.IncreaseIndent();

				foreach (var field in SharedFields)
				{
					field.WriteYaml(writer);
				}

				writer.DecreaseIndent();
			}

			if (Languages.Any())
			{
				writer.WriteMap("Languages");
				writer.IncreaseIndent();

				foreach (var language in Languages)
				{
					language.WriteYaml(writer);
				}

				writer.DecreaseIndent();
			}
		}

		public virtual void ReadYaml(YamlReader reader)
		{
			Id = reader.ReadExpectedGuidMap("ID");
			ParentId = reader.ReadExpectedGuidMap("Parent");
			TemplateId = reader.ReadExpectedGuidMap("Template");
			Path = reader.ReadExpectedMap("Path");

			var sharedFields = reader.PeekMap();
			if (sharedFields.HasValue && sharedFields.Value.Key.Equals("SharedFields"))
			{
				reader.ReadMap();
				while (true)
				{
					var field = new YamlFieldValue();
					if (field.ReadYaml(reader)) SharedFields.Add(field);
					else break;
				}
			}

			var languages = reader.PeekMap();
			if (languages.HasValue && languages.Value.Key.Equals("Languages"))
			{
				reader.ReadMap();
				while (true)
				{
					var language = new YamlLanguage();
					if (language.ReadYaml(reader)) Languages.Add(language);
					else break;
				}
			}
		}
	}
}
