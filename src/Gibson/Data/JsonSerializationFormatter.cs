using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Gibson.Data.FieldFormatters;
using Gibson.Data.Json;
using Gibson.Indexing;
using Gibson.Model;
using Newtonsoft.Json;
using Sitecore.Diagnostics;

namespace Gibson.Data
{
	public class JsonSerializationFormatter : ISerializationFormatter
	{
		private readonly List<IFieldFormatter> _fieldFormatters = new List<IFieldFormatter>();
		public List<IFieldFormatter> FieldFormatters
		{
			get
			{
				return _fieldFormatters;
			}
		}


		public ISerializableItem ReadSerializedItem(Stream dataStream)
		{
			Assert.ArgumentNotNull(dataStream, "dataStream");

			var serializer = new JsonSerializer();

			using (var sr = new StreamReader(dataStream))
			using (var jsonTextReader = new JsonTextReader(sr))
			{
				JsonItem itemResult = serializer.Deserialize<JsonItem>(jsonTextReader);

				return new JsonSerializableItem(itemResult);
			}
		}

		public void WriteSerializedItem(ISerializableItem item, Stream outputStream)
		{
			Assert.ArgumentNotNull(item, "item");
			Assert.ArgumentNotNull(outputStream, "outputStream");

			var itemToSerialize = new JsonItem();
			itemToSerialize.LoadFrom(item, FieldFormatters.ToArray());

			var serializer = new JsonSerializer();
			serializer.Formatting = Formatting.Indented;

			using (var sr = new StreamWriter(outputStream))
			using (var jsonWriter = new JsonTextWriter(sr))
			{
				serializer.Serialize(jsonWriter, itemToSerialize);
			}
		}

		protected class JsonSerializableItem : ISerializableItem
		{
			private readonly JsonItem _item;

			public JsonSerializableItem(JsonItem item)
			{
				_item = item;
			}

			public Guid Id
			{
				get { return _item.Id; }
			}

			public string DatabaseName
			{
				get { return _item.DatabaseName; }
			}

			public Guid ParentId { get; protected set; }

			public string Path { get; protected set; }

			public string Name
			{
				get { return _item.Name; }
			}

			public Guid BranchId
			{
				get { return _item.BranchId; }
			}

			public Guid TemplateId { get; protected set; }

			public IEnumerable<ISerializableFieldValue> SharedFields
			{
				get
				{
					return _item.SharedFields.Select(field => new JsonSerializableFieldValue(field));
				}
			}

			public IEnumerable<ISerializableVersion> Versions
			{
				get
				{
					foreach (var language in _item.Languages)
					{
						foreach (var version in language.Versions)
						{
							yield return new JsonSerializableVersion(version, language);
						}
					}
				}
			}

			public void AddIndexData(IndexEntry indexEntry)
			{
				Assert.ArgumentNotNull(indexEntry, "indexEntry");
				Assert.ArgumentCondition(indexEntry.Id == Id, "indexEntry", "The index entry to merge must have the same ID as the item being merged to.");

				ParentId = indexEntry.ParentId;
				Path = indexEntry.Path;
				TemplateId = indexEntry.TemplateId;
			}
		}

		protected class JsonSerializableVersion : ISerializableVersion
		{
			private readonly JsonVersion _version;
			private readonly JsonLanguage _language;

			public JsonSerializableVersion(JsonVersion version, JsonLanguage language)
			{
				_version = version;
				_language = language;
			}

			public IEnumerable<ISerializableFieldValue> Fields
			{
				get { return _version.Fields.Select(x => new JsonSerializableFieldValue(x)); }
			}

			public CultureInfo Language
			{
				get { return new CultureInfo(_language.Language); }
			}

			public int VersionNumber
			{
				get { return _version.VersionNumber; }
			}
		}

		protected class JsonSerializableFieldValue : ISerializableFieldValue
		{
			private readonly JsonFieldValue _field;

			public JsonSerializableFieldValue(JsonFieldValue field)
			{
				_field = field;
			}

			public Guid FieldId
			{
				get { return _field.Id; }
			}

			public string Value
			{
				get { return _field.Value; }
			}

			public string FieldType
			{
				get { return _field.Type; }
			}
		}
	}
}
