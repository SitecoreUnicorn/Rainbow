using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using Rainbow.Filtering;
using Rainbow.Formatting;
using Rainbow.Formatting.FieldFormatters;
using Rainbow.Indexing;
using Rainbow.Model;
using Rainbow.Storage.Yaml.Formatting.OutputModel;
using Sitecore.Configuration;
using Sitecore.Diagnostics;

namespace Rainbow.Storage.Yaml.Formatting
{
	public class YamlSerializationFormatter : ISerializationFormatter
	{
		private readonly IFieldFilter _fieldFilter;

		public YamlSerializationFormatter(XmlNode configNode, IFieldFilter fieldFilter)
		{
			Assert.ArgumentNotNull(configNode, "configNode");
			Assert.ArgumentNotNull(fieldFilter, "fieldFilter");

			_fieldFilter = fieldFilter;

			var formatters = configNode.ChildNodes;

			foreach (XmlNode formatter in formatters)
			{
				if (formatter.NodeType == XmlNodeType.Element && formatter.Name.Equals("fieldFormatter")) FieldFormatters.Add(Factory.CreateObject<IFieldFormatter>(formatter));
			}
		}

		private readonly List<IFieldFormatter> _fieldFormatters = new List<IFieldFormatter>();
		public List<IFieldFormatter> FieldFormatters
		{
			get
			{
				return _fieldFormatters;
			}
		}


		public virtual IItemData ReadSerializedItem(Stream dataStream, string serializedItemId)
		{
			Assert.ArgumentNotNull(dataStream, "dataStream");


			using (var reader = new YamlReader(dataStream, 4096, true))
			{
				var item = new YamlItem();
				item.ReadYaml(reader);

				return new YamlItemData(item, serializedItemId, FieldFormatters.ToArray());
			}
		}

		public virtual void WriteSerializedItem(IItemData itemData, Stream outputStream)
		{
			Assert.ArgumentNotNull(itemData, "item");
			Assert.ArgumentNotNull(outputStream, "outputStream");

			var filteredItem = new FilteredItem(itemData, _fieldFilter);

			var itemToSerialize = new YamlItem();
			itemToSerialize.LoadFrom(filteredItem, FieldFormatters.ToArray());

			using (var writer = new YamlWriter(outputStream, 4096, true))
			{
				itemToSerialize.WriteYaml(writer);
			}
		}

		[DebuggerDisplay("{Name} ({DatabaseName}::{Id}) [YAML - {SerializedItemId}]")]
		protected class YamlItemData : IItemData
		{
			private readonly YamlItem _item;
			private readonly string _serializedItemId;
			private readonly IFieldFormatter[] _formatters;
			private IndexEntry _indexEntry;

			public YamlItemData(YamlItem item, string serializedItemId, IFieldFormatter[] formatters)
			{
				_item = item;
				_serializedItemId = serializedItemId;
				_formatters = formatters;
				_indexEntry = new IndexEntry
				{
					Id = item.Id,
					ParentId = item.ParentId,
					TemplateId = item.TemplateId,
					Path = item.Path
				};
			}

			public Guid Id { get { return _indexEntry.Id; } }

			/// <remarks>The storage provider should set this automatically.</remarks>
			public string DatabaseName { get; set; }

			public Guid ParentId { get { return _indexEntry.ParentId; } }

			public string Path { get { return _indexEntry.Path; } }

			public string Name { get { return _indexEntry.Name; } }

			public Guid BranchId
			{
				get { return _item.BranchId; }
			}

			public Guid TemplateId { get { return _indexEntry.TemplateId; } }

			public IEnumerable<IItemFieldValue> SharedFields
			{
				get
				{
					return _item.SharedFields.Select(field => new YamlItemFieldValue(field, _formatters));
				}
			}

			public IEnumerable<IItemVersion> Versions
			{
				get
				{
					foreach (var language in _item.Languages)
					{
						foreach (var version in language.Versions)
						{
							yield return new YamlSerializableVersion(version, language, _formatters);
						}
					}
				}
			}

			public string SerializedItemId
			{
				get { return _serializedItemId; }
			}

			public void AddIndexData(IndexEntry indexEntry)
			{
				Assert.ArgumentNotNull(indexEntry, "indexEntry");

				_indexEntry = indexEntry;
			}
		}

		protected class YamlSerializableVersion : IItemVersion
		{
			private readonly YamlVersion _version;
			private readonly YamlLanguage _language;
			private readonly IFieldFormatter[] _formatters;

			public YamlSerializableVersion(YamlVersion version, YamlLanguage language, IFieldFormatter[] formatters)
			{
				_version = version;
				_language = language;
				_formatters = formatters;
			}

			public IEnumerable<IItemFieldValue> Fields
			{
				get { return _version.Fields.Select(x => new YamlItemFieldValue(x, _formatters)); }
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

		protected class YamlItemFieldValue : IItemFieldValue
		{
			private readonly YamlFieldValue _field;
			private readonly IFieldFormatter[] _formatters;

			public YamlItemFieldValue(YamlFieldValue field, IFieldFormatter[] formatters)
			{
				_field = field;
				_formatters = formatters;
			}

			public Guid FieldId
			{
				get { return _field.Id; }
			}

			private string _value;
			public string Value
			{
				get
				{
					if (_value == null)
					{
						_value = _field.Value;

						foreach (var formatter in _formatters)
						{
							if (formatter.CanFormat(this))
							{
								_value = formatter.Unformat(_value);

								break;
							}
						}
					}
					return _value;
				}
			}

			public string FieldType
			{
				get { return _field.Type; }
			}

			public string NameHint
			{
				get { return _field.NameHint; }
			}
		}
	}
}
