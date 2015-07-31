using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using Rainbow.Filtering;
using Rainbow.Formatting;
using Rainbow.Formatting.FieldFormatters;
using Rainbow.Model;
using Rainbow.Storage.Yaml.OutputModel;
using Sitecore.Configuration;
using Sitecore.Diagnostics;

namespace Rainbow.Storage.Yaml
{
	public class YamlSerializationFormatter : ISerializationFormatter
	{
		private readonly IFieldFilter _fieldFilter;

		public YamlSerializationFormatter(XmlNode configNode, IFieldFilter fieldFilter)
		{
			_fieldFilter = fieldFilter;

			if (configNode == null) return;

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

			try
			{
				using (var reader = new YamlReader(dataStream, 4096, true))
				{
					var item = new YamlItem();
					item.ReadYaml(reader);

					return new YamlItemData(item, serializedItemId, FieldFormatters.ToArray(), ParentDataStore);
				}
			}
			catch (Exception exception)
			{
				throw new FormatException("Error reading " + serializedItemId, exception);
			}
		}

		public IItemMetadata ReadSerializedItemMetadata(Stream dataStream, string serializedItemId)
		{
			Assert.ArgumentNotNull(dataStream, "dataStream");

			try
			{
				using (var reader = new YamlReader(dataStream, 4096, true))
				{
					var item = new YamlItemMetadata();
					item.ReadYaml(reader, serializedItemId);

					return item;
				}
			}
			catch (Exception exception)
			{
				throw new FormatException("Error reading " + serializedItemId, exception);
			}
		}

		public virtual void WriteSerializedItem(IItemData itemData, Stream outputStream)
		{
			Assert.ArgumentNotNull(itemData, "item");
			Assert.ArgumentNotNull(outputStream, "outputStream");

			if(_fieldFilter != null)
				itemData = new FilteredItem(itemData, _fieldFilter);

			var itemToSerialize = new YamlItem();
			itemToSerialize.LoadFrom(itemData, FieldFormatters.ToArray());

			using (var writer = new YamlWriter(outputStream, 4096, true))
			{
				itemToSerialize.WriteYaml(writer);
			}
		}

		public string FileExtension { get { return ".yml"; } }
		[ExcludeFromCodeCoverage]
		public IDataStore ParentDataStore { get; set; }


		[DebuggerDisplay("{Name} ({DatabaseName}::{Id}) [YAML - {SerializedItemId}]")]
		protected class YamlItemData : IItemData
		{
			private readonly YamlItem _item;
			private readonly string _serializedItemId;
			private readonly IFieldFormatter[] _formatters;
			private readonly IDataStore _sourceDataStore;

			public YamlItemData(YamlItem item, string serializedItemId, IFieldFormatter[] formatters, IDataStore sourceDataStore)
			{
				_item = item;
				_serializedItemId = serializedItemId;
				_formatters = formatters;
				_sourceDataStore = sourceDataStore;
			}

			public Guid Id { get { return _item.Id; } }

			/// <remarks>The storage provider should set this automatically.</remarks>
			[ExcludeFromCodeCoverage]
			public string DatabaseName { get; set; }

			public Guid ParentId { get { return _item.ParentId; } }

			public string Path { get { return _item.Path; } }

			public string Name
			{
				get
				{
					if (Path == null) return null;

					var trimmedPath = Path.TrimEnd('/');

					var index = trimmedPath.LastIndexOf('/');

					if (index < 0) return Path;

					return trimmedPath.Substring(index + 1);
				}
			}

			[ExcludeFromCodeCoverage]
			public Guid BranchId
			{
				get { return _item.BranchId; }
			}

			[ExcludeFromCodeCoverage]
			public Guid TemplateId { get { return _item.TemplateId; } }

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

			[ExcludeFromCodeCoverage]
			public IEnumerable<IItemData> GetChildren()
			{
				return _sourceDataStore.GetChildren(this);
			}
		}

		[ExcludeFromCodeCoverage]
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

			[ExcludeFromCodeCoverage]
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

			[ExcludeFromCodeCoverage]
			public string FieldType
			{
				get { return _field.Type; }
			}

			[ExcludeFromCodeCoverage]
			public string NameHint
			{
				get { return _field.NameHint; }
			}
		}
	}
}
