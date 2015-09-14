using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Rainbow.Model;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;

namespace Rainbow.Storage.Sc
{
	[DebuggerDisplay("{Name} ({DatabaseName}::{Id}) [DB ITEM]")]
	public class ItemData : IItemData
	{
		private readonly Item _item;
		private readonly IDataStore _sourceDataStore;
		private Item[] _itemVersions;
		// ReSharper disable once RedundantDefaultMemberInitializer
		private bool _fieldsLoaded = false;

		public ItemData(Item item)
		{
			_item = item;
		}

		public ItemData(Item item, IDataStore sourceDataStore) : this(item)
		{
			_sourceDataStore = sourceDataStore;
		}

		public virtual Guid Id
		{
			get { return _item.ID.Guid; }
		}

		public virtual string DatabaseName
		{
			get { return _item.Database.Name; }
			set { }
		}

		public virtual Guid ParentId
		{
			get { return _item.ParentID.Guid; }
		}

		private string _path;
		public virtual string Path
		{
			get
			{
				if (_path == null) return _path = _item.Paths.Path;
				return _path;
			}
		}

		public virtual string Name
		{
			get { return _item.Name; }
		}

		public virtual Guid BranchId
		{
			get { return _item.BranchId.Guid; }
		}

		public virtual Guid TemplateId
		{
			get { return _item.TemplateID.Guid; }
		}

		private List<IItemFieldValue> _sharedFields;
		public virtual IEnumerable<IItemFieldValue> SharedFields
		{
			get
			{
				if (_sharedFields == null)
				{
					EnsureFields();

					var template = TemplateManager.GetTemplate(_item);

					if (template == null)
					{
						Log.Warn("Unable to read shared fields for {0} because template {1} did not exist.".FormatWith(_item.ID, _item.TemplateID), this);
						return Enumerable.Empty<IItemFieldValue>();
					}

					var fieldResults = new List<IItemFieldValue>();

					for (int i = 0; i < _item.Fields.Count; i++)
					{
						var field = _item.Fields[i];

						// no versioned fields or fields not on the template
						if (!field.Shared || template.GetField(field.ID) == null) continue;

						var value = field.GetValue(false, false);

						if (value != null)
							fieldResults.Add(CreateFieldValue(field, value));
					}

					_sharedFields = fieldResults;
				}

				return _sharedFields;
			}
		}

		private List<IItemVersion> _versions;
		public virtual IEnumerable<IItemVersion> Versions
		{
			get
			{
				if (_versions == null)
				{
					var versionResults = new List<IItemVersion>();

					var versions = GetVersions();
					for (int i = 0; i < versions.Length; i++)
					{
						versionResults.Add(CreateVersion(versions[i]));
					}

					_versions = versionResults;
				}

				return _versions;
			}
		}

		public virtual string SerializedItemId
		{
			get { return "(from Sitecore database)"; }
		}

		public virtual IEnumerable<IItemData> GetChildren()
		{
			if (_sourceDataStore != null)
				return _sourceDataStore.GetChildren(this);

			return _item.GetChildren().Select(child => new ItemData(child));
		}

		protected virtual void EnsureFields()
		{
			if (!_fieldsLoaded)
			{
				_item.Fields.ReadAll();
				_fieldsLoaded = true;
			}
		}

		protected virtual Item[] GetVersions()
		{
			if (_itemVersions == null)
				_itemVersions = _item.Versions.GetVersions(true);

			return _itemVersions;
		}

		protected virtual IItemFieldValue CreateFieldValue(Field field, string value)
		{
			return new ItemFieldValue(field, value);
		}

		protected virtual IItemVersion CreateVersion(Item version)
		{
			return new ItemVersionValue(version);
		}

		[DebuggerDisplay("{NameHint} ({FieldType})")]
		protected class ItemFieldValue : IItemFieldValue
		{
			private readonly Field _field;
			private readonly string _retrievedStringValue;

			public ItemFieldValue(Field field, string retrievedStringValue)
			{
				_field = field;
				_retrievedStringValue = retrievedStringValue;
			}

			public Guid FieldId
			{
				get { return _field.ID.Guid; }
			}

			public virtual string Value
			{
				get
				{
					if (_field.IsBlobField)
					{
						if (!_field.HasBlobStream) return null;

						using (var stream = _field.GetBlobStream())
						{
							var buf = new byte[stream.Length];

							stream.Read(buf, 0, (int)stream.Length);

							return Convert.ToBase64String(buf);
						}
					}
					return _retrievedStringValue;
				}
			}

			public string FieldType
			{
				get { return _field.Type; }
			}

			public virtual Guid? BlobId
			{
				get
				{
					if (_field.IsBlobField)
					{
						string parsedIdValue = _field.Value;
						if (parsedIdValue.Length > 38)
							parsedIdValue = parsedIdValue.Substring(0, 38);

						Guid blobId;
						if(Guid.TryParse(parsedIdValue, out blobId)) return blobId;
					}

					return null;
				}
			}

			public string NameHint
			{
				get { return _field.Name; }
			}
		}

		[DebuggerDisplay("{Language} #{VersionNumber}")]
		protected class ItemVersionValue : IItemVersion
		{
			private readonly Item _version;
			// ReSharper disable once RedundantDefaultMemberInitializer
			private bool _fieldsLoaded = false;

			public ItemVersionValue(Item version)
			{
				_version = version;
			}

			private List<IItemFieldValue> _fields;

			public virtual IEnumerable<IItemFieldValue> Fields
			{
				get
				{
					if (_fields == null)
					{
						EnsureFields();

						var template = TemplateManager.GetTemplate(_version);

						if (template == null)
						{
							Log.Warn("Unable to read shared fields for {0} because template {1} did not exist.".FormatWith(_version.ID, _version.TemplateID), this);
							return Enumerable.Empty<IItemFieldValue>();
						}

						var fieldResults = new List<IItemFieldValue>();

						for (int i = 0; i < _version.Fields.Count; i++)
						{
							var field = _version.Fields[i];

							// no shared fields or fields not on the template
							if (field.Shared || template.GetField(field.ID) == null) continue;

							var value = field.GetValue(false, false);

							if (value != null)
								fieldResults.Add(CreateFieldValue(field, value));
						}

						_fields = fieldResults;
					}

					return _fields;
				}
			}

			public CultureInfo Language { get { return _version.Language.CultureInfo; } }

			public int VersionNumber
			{
				get { return _version.Version.Number; }
			}

			protected virtual void EnsureFields()
			{
				if (!_fieldsLoaded)
				{
					_version.Fields.ReadAll();
					_fieldsLoaded = true;
				}
			}
			protected virtual IItemFieldValue CreateFieldValue(Field field, string value)
			{
				return new ItemFieldValue(field, value);
			}
		}
	}
}
