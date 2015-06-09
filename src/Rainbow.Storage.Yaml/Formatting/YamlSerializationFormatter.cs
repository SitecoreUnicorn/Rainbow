using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
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
		public YamlSerializationFormatter()
		{

		}

		public YamlSerializationFormatter(XmlNode configNode)
		{
			Assert.ArgumentNotNull(configNode, "configNode");

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


		public virtual ISerializableItem ReadSerializedItem(Stream dataStream, string serializedItemId)
		{
			Assert.ArgumentNotNull(dataStream, "dataStream");


			using (var reader = new YamlReader(dataStream, 4096, true))
			{
				var item = new YamlItem();
				item.ReadYaml(reader);

				return new YamlSerializableItem(item, serializedItemId, FieldFormatters.ToArray());
			}
		}

		public virtual void WriteSerializedItem(ISerializableItem item, Stream outputStream)
		{
			Assert.ArgumentNotNull(item, "item");
			Assert.ArgumentNotNull(outputStream, "outputStream");

			var itemToSerialize = new YamlItem();
			itemToSerialize.LoadFrom(item, FieldFormatters.ToArray());

			using (var writer = new YamlWriter(outputStream, 4096, true))
			{
				itemToSerialize.WriteYaml(writer);
			}
		}

		[DebuggerDisplay("{Name} ({DatabaseName}::{Id}) [YAML - {SerializedItemId}]")]
		protected class YamlSerializableItem : ISerializableItem
		{
			private readonly YamlItem _item;
			private readonly string _serializedItemId;
			private readonly IFieldFormatter[] _formatters;
			private IndexEntry _indexEntry;

			public YamlSerializableItem(YamlItem item, string serializedItemId, IFieldFormatter[] formatters)
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

			public IEnumerable<ISerializableFieldValue> SharedFields
			{
				get
				{
					return _item.SharedFields.Select(field => new YamlSerializableFieldValue(field, _formatters));
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

		protected class YamlSerializableVersion : ISerializableVersion
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

			public IEnumerable<ISerializableFieldValue> Fields
			{
				get { return _version.Fields.Select(x => new YamlSerializableFieldValue(x, _formatters)); }
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

		protected class YamlSerializableFieldValue : ISerializableFieldValue
		{
			private readonly YamlFieldValue _field;
			private readonly IFieldFormatter[] _formatters;

			public YamlSerializableFieldValue(YamlFieldValue field, IFieldFormatter[] formatters)
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
						foreach (var formatter in _formatters)
						{
							if (formatter.CanFormat(this))
							{
								_value = formatter.Unformat(_value ?? _field.Value);

								break;
							}
						}
					}
					return _field.Value;
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
