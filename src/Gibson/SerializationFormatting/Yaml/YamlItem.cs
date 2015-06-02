using System;
using System.Collections.Generic;
using System.Linq;
using Gibson.Model;
using Gibson.SerializationFormatting.FieldFormatters;

namespace Gibson.SerializationFormatting.Yaml
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

		public string DatabaseName { get; set; }
		public Guid BranchId { get; set; }
		public SortedSet<YamlFieldValue> SharedFields { get; private set; }
		public SortedSet<YamlLanguage> Languages { get; private set; }

		public virtual void LoadFrom(ISerializableItem item, IFieldFormatter[] fieldFormatters)
		{
			Id = item.Id;
			ParentId = item.ParentId;
			TemplateId = item.TemplateId;
			Path = item.Path;

			DatabaseName = item.DatabaseName;
			BranchId = item.BranchId;

			foreach (var field in item.SharedFields)
			{
				if(string.IsNullOrWhiteSpace(field.Value)) continue;

				var fieldObject = new YamlFieldValue();
				fieldObject.LoadFrom(field, fieldFormatters);

				SharedFields.Add(fieldObject);
			}

			var languages = item.Versions.GroupBy(x => x.Language.Name);

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
	}
}
