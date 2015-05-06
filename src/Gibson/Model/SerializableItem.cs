using System;
using System.Collections.Generic;
using System.Globalization;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;

namespace Gibson.Model
{
	public class SerializableItem : ISerializableItem
	{
		private readonly Item _item;
		private Item[] _itemVersions;
		private bool _fieldsLoaded = false;

		public SerializableItem(Item item)
		{
			_item = item;
		}

		public Guid Id
		{
			get { return _item.ID.Guid; }
		}

		public string DatabaseName
		{
			get { return _item.Database.Name; }
		}

		public Guid ParentId
		{
			get { return _item.ParentID.Guid; }
		}

		public string Path
		{
			get { return _item.Paths.FullPath; }
		}

		public string Name
		{
			get { return _item.Name; }
		}

		public Guid BranchId
		{
			get { return _item.BranchId.Guid; }
		}

		public Guid TemplateId
		{
			get { return _item.TemplateID.Guid; }
		}

		public IEnumerable<ISerializableFieldValue> SharedFields
		{
			get
			{
				EnsureFields();

				for (int i = 0; i < _item.Fields.Count; i++)
				{
					var field = _item.Fields[i];
					if (field.Shared && !field.ContainsStandardValue)
						yield return new ItemFieldValue(field);
				}
			}
		}

		public IEnumerable<ISerializableVersion> Versions
		{
			get
			{
				var versions = GetVersions();
				for (int i = 0; i < versions.Length; i++)
				{
					yield return new ItemVersionValue(versions[i]);
				}
			}
		}

		protected void EnsureFields()
		{
			if (!_fieldsLoaded)
			{
				_item.Fields.ReadAll();
				_fieldsLoaded = true;
			}
		}

		protected Item[] GetVersions()
		{
			if (_itemVersions == null)
				_itemVersions = _item.Versions.GetVersions(true);

			return _itemVersions;
		}

		protected class ItemFieldValue : ISerializableFieldValue
		{
			private readonly Field _field;

			public ItemFieldValue(Field field)
			{
				_field = field;
			}

			public Guid FieldId
			{
				get { return _field.ID.Guid; }
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

		protected class ItemVersionValue : ISerializableVersion
		{
			private readonly Item _version;
			private bool _fieldsLoaded = false;

			public ItemVersionValue(Item version)
			{
				_version = version;
			}

			public IEnumerable<ISerializableFieldValue> Fields
			{
				get
				{
					EnsureFields();

					for (int i = 0; i < _version.Fields.Count; i++)
					{
						var field = _version.Fields[i];
						if (!field.Shared && !field.ContainsStandardValue) // unversioned fields may be duplicated across versions doing this?
							yield return new ItemFieldValue(field);
					}
				}
			}

			public CultureInfo Language { get { return _version.Language.CultureInfo; } }

			public int VersionNumber
			{
				get { return _version.Version.Number; }
			}

			protected void EnsureFields()
			{
				if (!_fieldsLoaded)
				{
					_version.Fields.ReadAll();
					_fieldsLoaded = true;
				}
			}
		}
	}
}
